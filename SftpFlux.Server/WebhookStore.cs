namespace SftpFlux.Server
{
    public static class WebhookStore
    {
        public static List<WebhookSubscription> Subscriptions { get; } = new();
    }
}
