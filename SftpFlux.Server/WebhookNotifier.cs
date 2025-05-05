using System.Text.Json;
using System.Text;

namespace SftpFlux.Server
{
    public class WebhookNotifier
    {

        public static async Task NotifyWebhooksAsync(string eventType, object payload)
        {
            var webhooks = WebhookStore.Subscriptions.Where(s => s.EventType == eventType);

            foreach (var webhook in webhooks)
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync(webhook.Url, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify webhook {webhook.Url}: {ex.Message}");
                }
            }
        }
    }
}
