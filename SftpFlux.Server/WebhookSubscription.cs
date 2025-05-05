namespace SftpFlux.Server
{
    public class WebhookSubscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Url { get; set; } = string.Empty;
        public string EventType { get; set; } = "file.created"; // Extendable for other events
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
