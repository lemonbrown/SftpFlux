using Renci.SshNet;

namespace SftpFlux.Server
{
    public class SftpPollingService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public SftpPollingService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var sftpClient = scope.ServiceProvider.GetRequiredService<SftpClient>();
                var poller = new SftpPoller(sftpClient, "/upload");

                await poller.PollSftpAndDetectChangesAsync();

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Adjustable interval
            }
        }
    }

}
