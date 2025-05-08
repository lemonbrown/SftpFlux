namespace SftpFlux.Server.Polling.Webhooks {
    public static class WebhookStore {
        public static List<WebhookSubscription> Subscriptions { get; } = new();
    }
}
