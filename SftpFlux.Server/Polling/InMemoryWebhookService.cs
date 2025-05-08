using SftpFlux.Server.Authorization;
using SftpFlux.Server.Helpers;

namespace SftpFlux.Server.Polling {

    public class InMemoryWebhookService : IWebhookService {

        private readonly List<WebhookSubscription> _subs = new();

        public Task<List<WebhookSubscription>> GetWebhooksForSftpAsync(string sftpId)
            => Task.FromResult(_subs.Where(s => s.SftpId == sftpId).ToList());

        public Task<WebhookSubscription?> GetAsync(Guid id)
            => Task.FromResult(_subs.FirstOrDefault(s => s.Id == id));

        public Task AddAsync(WebhookSubscription subscription, ApiKey? apiKey) {

            if(apiKey != null)
                if (!PathSecurity.AreAllRequestedPathsAllowed(subscription.IncludePaths, apiKey.WebhookAllowedPaths))
                    throw new InvalidOperationException("Requested include paths not allowed by API key.");

            subscription.ApiKey = apiKey?.Key ?? "";

            _subs.Add(subscription);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id) {
            _subs.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }
    }

}
