using System.Text.Json;
using System.Text;
using SftpFlux.Server.Helpers;

namespace SftpFlux.Server.Polling {

    public class WebhookNotifier(IWebhookService webhookService) {

        public async Task NotifyWebhooksAsync(string sftpId, string eventType, object payload) {

            var webhooks = await webhookService.GetWebhooksForSftpAsync(sftpId);

            foreach (var webhook in webhooks) {

                var payload2 = (dynamic)payload;

                if (!PathSecurity.IsPathAllowed(payload2.path, webhook.IncludePaths))
                    continue;

                try { 
                    using var httpClient = new HttpClient();
                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync(webhook.Url, content);
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to notify webhook {webhook.Url}: {ex.Message}");
                }
            }
        }
    }
}
