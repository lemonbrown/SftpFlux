namespace SftpFlux.Server.Polling {
    public static class WebhookStore {
        public static List<WebhookSubscription> Subscriptions { get; } = new();
    }
}
