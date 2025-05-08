namespace SftpFlux.Server.Polling.Webhooks {

    public class WebhookSubscription {

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Url { get; set; } = string.Empty;

        public List<FileChangeType> SubscribedEvents { get; set; } = [];

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string SftpId { get; set; } = "";

        public string ApiKey { get; set; } = "";

        public List<string> IncludePaths { get; set; } = [];

        public string QueryUrl { get; set; } = default!;

        public string? FileNameRegex { get; set; }

        public string? QueueName { get; set; }

    }

}
