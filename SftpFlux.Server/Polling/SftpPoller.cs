using Renci.SshNet;
using SftpFlux.Server.Connection;

namespace SftpFlux.Server.Polling {

    public class SftpPoller(SftpConnectionInfo sftpConnectionInfo, SftpClient sftpClient, string watchPath, WebhookNotifier webhookNotifier) {

        private readonly HashSet<string> _seenFiles = new();

        public async Task PollSftpAndDetectChangesAsync() {
            if (!sftpClient.IsConnected)
                sftpClient.Connect();

            var files = sftpClient.ListDirectory(watchPath)
                .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
                .ToList();

            foreach (var file in files) {
                var fullPath = file.FullName;

                if (!_seenFiles.Contains(fullPath)) {
                    _seenFiles.Add(fullPath);

                    var fileEvent = new {
                        fileName = file.Name,
                        path = file.FullName,
                        size = file.Attributes.Size,
                        lastModified = file.Attributes.LastWriteTimeUtc,
                        detectedAt = DateTime.UtcNow
                    };

                    Console.WriteLine($"[+] New file detected: {file.Name}");
                    await webhookNotifier.NotifyWebhooksAsync(sftpConnectionInfo.Id, "file.created", fileEvent);
                }
            }
        }
    }

}
