namespace SftpFlux.Server.Queues {
    public interface IFileQueueService {
        Task EnqueueAsync(QueuedFileItem item);
        Task<QueuedFileItem?> DequeueAsync(string queueName);
        Task<List<QueuedFileItem>> PeekAsync(string queueName, int count = 10);
    }
}
