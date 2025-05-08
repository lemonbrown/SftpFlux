namespace SftpFlux.Server.Jobs {
    public class JobSchedulerService : BackgroundService {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobSchedulerService> _logger;

        public JobSchedulerService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<JobSchedulerService>>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ScheduledJobProcessor>();

                try {
                    await processor.RunDueJobsAsync();
                } catch (Exception ex) {
                    _logger.LogError(ex, "Scheduled job execution failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30s
            }
        }
    }

}
