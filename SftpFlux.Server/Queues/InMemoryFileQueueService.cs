using System.Collections.Concurrent;

namespace SftpFlux.Server.Queues {

    public class InMemoryFileQueueService : IFileQueueService {

        private readonly ConcurrentDictionary<string, ConcurrentQueue<QueuedFileItem>> _queues = new();

        public Task EnqueueAsync(QueuedFileItem item) {
            var queue = _queues.GetOrAdd(item.QueueName, _ => new ConcurrentQueue<QueuedFileItem>());
            queue.Enqueue(item);
            return Task.CompletedTask;
        }

        public Task<QueuedFileItem?> DequeueAsync(string queueName) {
            if (_queues.TryGetValue(queueName, out var queue) && queue.TryDequeue(out var item))
                return Task.FromResult<QueuedFileItem?>(item);

            return Task.FromResult<QueuedFileItem?>(null);
        }

        public Task<List<QueuedFileItem>> PeekAsync(string queueName, int count = 10) {
            if (_queues.TryGetValue(queueName, out var queue))
                return Task.FromResult(queue.Take(count).ToList());

            return Task.FromResult(new List<QueuedFileItem>());
        }
    }
}
