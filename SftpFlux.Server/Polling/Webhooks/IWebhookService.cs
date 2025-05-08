using SftpFlux.Server.Authorization;

namespace SftpFlux.Server.Polling.Webhooks {
    public interface IWebhookService {
        Task<List<WebhookSubscription>> GetWebhooksForSftpAsync(string sftpId);
        Task<WebhookSubscription?> GetAsync(Guid id);
        Task AddAsync(WebhookSubscription subscription, ApiKey? apiKey);
        Task DeleteAsync(Guid id);
    }
}
