using Microsoft.Extensions.Hosting;
using Renci.SshNet;
using SftpFlux.Server.Connection;
using SftpFlux.Server.Polling.Webhooks;
using SftpFlux.Server.Queries;
using SftpFlux.Server.Queues;

namespace SftpFlux.Server.Polling {

    public class SftpPollingService(
        IServiceProvider services) : BackgroundService {

        private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;

        private readonly HashSet<string> _seenFiles = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

            while (!stoppingToken.IsCancellationRequested) {

                using var scope = services.CreateScope();

                var sftpRegistry = scope.ServiceProvider.GetRequiredService<ISftpConnectionRegistry>();

                var webhookNotifier = scope.ServiceProvider.GetRequiredService<WebhookNotifier>();

                var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

                var fileQueryService = scope.ServiceProvider.GetRequiredService<IFileQueryService>();

                var queueService = scope.ServiceProvider.GetRequiredService<IFileQueueService>();

                var connections = sftpRegistry.GetAll();

                foreach(var connection in connections) {

                    var subs = await webhookService.GetWebhooksForSftpAsync(connection.Id);

                    foreach (var sub in subs) {

                        var fileQuery = FileQueryBinder.FromUrl(sub.QueryUrl);

                        var queryResult = await fileQueryService.QueryFilesAsync(fileQuery);

                        if (sub.QueueName != null) {

                            foreach (var result in queryResult.Results) {

                                var fullPath = result.FullPath;

                                if (!_seenFiles.Contains(fullPath) && result.LastModifiedUtc >= _startTime) {

                                    _seenFiles.Add(fullPath);

                                    var queueItem = new QueuedFileItem {
                                        EnqueuedAtUtc = DateTime.UtcNow,
                                        FileName = result.Name,
                                        Path = result.Path,
                                        QueueName = sub.QueueName
                                    };

                                    await queueService.EnqueueAsync(queueItem);
                                }
                             }

                        } else {

                            await webhookNotifier.NotifyWebhookAsync(sub, queryResult);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Adjustable interval
            }
        }
    }

}
