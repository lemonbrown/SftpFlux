namespace SftpFlux.Server.Queues {

    public class QueuedFileItem {
        public string QueueName { get; set; } = "default";
        public string Path { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;
        public string? MetadataJson { get; set; }
    }

}
