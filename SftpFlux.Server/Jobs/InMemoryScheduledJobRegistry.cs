namespace SftpFlux.Server.Jobs {
    public class InMemoryScheduledJobRegistry : IScheduledJobRegistry {

        private readonly List<ScheduledFileJob> _jobs = new();

        public IReadOnlyCollection<ScheduledFileJob> GetAllJobs() => _jobs;

        public void AddJob(ScheduledFileJob job) {
            _jobs.Add(job);
        }
    }
}
