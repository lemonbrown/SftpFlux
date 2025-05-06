namespace SftpFlux.Server.Polling {

    public class WebhookSubscription {

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Url { get; set; } = string.Empty;

        public string EventType { get; set; } = "file.created"; // Extendable for other events

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string SftpId { get; set; } = "";

        public string ApiKey { get; set; } = "";

        // Optional: limit which paths this key can subscribe to
        public List<string> IncludePaths { get; set; } = new();
    }

}
