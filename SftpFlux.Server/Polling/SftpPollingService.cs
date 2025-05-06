using Microsoft.Extensions.Hosting;
using Renci.SshNet;
using SftpFlux.Server.Connection;

namespace SftpFlux.Server.Polling {

    public class SftpPollingService : BackgroundService {

        private readonly IServiceProvider _services;

        public SftpPollingService(IServiceProvider services) {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

            while (!stoppingToken.IsCancellationRequested) {

                using var scope = _services.CreateScope();

                var sftpRegistry = scope.ServiceProvider.GetRequiredService<ISftpConnectionRegistry>();

                var webhookNotifier = scope.ServiceProvider.GetRequiredService<WebhookNotifier>();

                var connections = sftpRegistry.GetAll();

                foreach(var connection in connections) {

                    var sftpClient = new SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);

                    var poller = new SftpPoller(connection, sftpClient, "/upload", webhookNotifier);

                    await poller.PollSftpAndDetectChangesAsync();
                }

                //var sftpClient = scope.ServiceProvider.GetRequiredService<SftpClient>();
                //var poller = new SftpPoller(sftpClient, "/upload");

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Adjustable interval
            }
        }
    }

}
