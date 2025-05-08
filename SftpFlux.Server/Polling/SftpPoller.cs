using Renci.SshNet;
using SftpFlux.Server.Connection;
using SftpFlux.Server.Polling.Webhooks;
using SftpFlux.Server.Queries;

namespace SftpFlux.Server.Polling {

    public class SftpPoller(
        SftpConnectionInfo sftpConnectionInfo,         
        IFileQueryService fileQueryService,
        FileQuery fileQuery, 
        WebhookNotifier webhookNotifier) {

        private readonly HashSet<string> _seenFiles = new();

        public async Task PollSftpAndDetectChangesAsync() {
            //if (!sftpClient.IsConnected)
            //    sftpClient.Connect();

            //var files = sftpClient.ListDirectory(watchPath)
            //    .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
            //    .ToList();

            var queryResult = await fileQueryService.QueryFilesAsync(fileQuery);

            foreach (var file in queryResult.Results) {

                var fullPath = file.FullPath;

                if (!_seenFiles.Contains(fullPath)) {
                    _seenFiles.Add(fullPath);

                    var fileEvent = new {
                        fileName = file.Name,
                        path = file.FullPath,
                        size = file.Size,
                        lastModified = file.LastModifiedUtc,
                        detectedAt = DateTime.UtcNow
                    };

                    Console.WriteLine($"[+] New file detected: {file.Name}");
                    //await webhookNotifier.NotifyWebhooksAsync(sftpConnectionInfo.Id, "file.created", fileEvent);
                }
            }

            //foreach (var file in files) {
            //    var fullPath = file.FullName;

            //    if (!_seenFiles.Contains(fullPath)) {
            //        _seenFiles.Add(fullPath);

            //        var fileEvent = new {
            //            fileName = file.Name,
            //            path = file.FullName,
            //            size = file.Attributes.Size,
            //            lastModified = file.Attributes.LastWriteTimeUtc,
            //            detectedAt = DateTime.UtcNow
            //        };

            //        Console.WriteLine($"[+] New file detected: {file.Name}");
            //        await webhookNotifier.NotifyWebhooksAsync(sftpConnectionInfo.Id, "file.created", fileEvent);
            //    }
            //}
        }
    }

}
