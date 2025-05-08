namespace SftpFlux.Server.Jobs {
    public interface IScheduledJobRegistry {
        IReadOnlyCollection<ScheduledFileJob> GetAllJobs();
        void AddJob(ScheduledFileJob job);
    }
}
