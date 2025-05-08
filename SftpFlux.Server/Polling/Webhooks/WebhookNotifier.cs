using System.Text.Json;
using System.Text;
using SftpFlux.Server.Helpers;
using SftpFlux.Server.Queries;
using Microsoft.AspNetCore;

namespace SftpFlux.Server.Polling.Webhooks {

    public class WebhookNotifier(IWebhookService webhookService) {

        //public async Task NotifyWebhooksAsync(string sftpId, string eventType, object payload) {

        //    var webhooks = await webhookService.GetWebhooksForSftpAsync(sftpId);

        //    foreach (var webhook in webhooks) {

        //        var payload2 = (dynamic)payload;

        //        if (!PathSecurity.IsPathAllowed(payload2.path, webhook.IncludePaths))
        //            continue;

        //        try {
        //            using var httpClient = new HttpClient();
        //            var json = JsonSerializer.Serialize(payload);
        //            var content = new StringContent(json, Encoding.UTF8, "application/json");
        //            await httpClient.PostAsync(webhook.Url, content);
        //        } catch (Exception ex) {
        //            Console.WriteLine($"Failed to notify webhook {webhook.Url}: {ex.Message}");
        //        }
        //    }
        //}

        public async Task NotifyWebhookAsync(WebhookSubscription subscription, FileQueryResult fileQueryResult) {

            var payload = fileQueryResult;

            try {
                using var httpClient = new HttpClient();
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await httpClient.PostAsync(subscription.Url, content);
            } catch (Exception ex) {
                Console.WriteLine($"Failed to notify webhook {subscription.Url}: {ex.Message}");
            }
        }
    }
}
