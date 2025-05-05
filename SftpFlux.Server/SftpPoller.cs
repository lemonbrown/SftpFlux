using Renci.SshNet;

namespace SftpFlux.Server
{
    public class SftpPoller
    {
        private readonly SftpClient _sftpClient;
        private readonly string _watchPath;
        private readonly HashSet<string> _seenFiles = new();

        public SftpPoller(SftpClient sftpClient, string watchPath)
        {
            _sftpClient = sftpClient;
            _watchPath = watchPath;
        }

        public async Task PollSftpAndDetectChangesAsync()
        {
            if (!_sftpClient.IsConnected)
                _sftpClient.Connect();

            var files = _sftpClient.ListDirectory(_watchPath)
                .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
                .ToList();

            foreach (var file in files)
            {
                var fullPath = file.FullName;

                if (!_seenFiles.Contains(fullPath))
                {
                    _seenFiles.Add(fullPath);

                    var fileEvent = new
                    {
                        fileName = file.Name,
                        path = file.FullName,
                        size = file.Attributes.Size,
                        lastModified = file.Attributes.LastWriteTimeUtc,
                        detectedAt = DateTime.UtcNow
                    };

                    Console.WriteLine($"[+] New file detected: {file.Name}");
                    await WebhookNotifier.NotifyWebhooksAsync("file.created", fileEvent);
                }
            }
        }
    }

}
