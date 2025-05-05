using System.Collections.Concurrent;

namespace SftpFlux.Server {
    public class InMemoryApiKeyService : IApiKeyService{
        private readonly ConcurrentDictionary<string, ApiKey> _apiKeyStore = new();

        public Task<string> CreateKeyAsync(List<string> scopes) {
            var entry = new ApiKey {
                Scopes = scopes
            };

            _apiKeyStore[entry.Key] = entry;

            return Task.FromResult(entry.Key);
        }

        public Task<bool> RevokeKeyAsync(string key) {
            if (_apiKeyStore.TryGetValue(key, out var entry)) {
                entry.IsRevoked = true;
                entry.RevokedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> ReinstateKeyAsync(string key) {
            if (_apiKeyStore.TryGetValue(key, out var entry)) {
                entry.IsRevoked = false;
                entry.RevokedAt = null;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<ApiKey?> GetKeyAsync(string key) {
            _apiKeyStore.TryGetValue(key, out var entry);
            return Task.FromResult(entry);
        }

        public Task<bool> DeleteKeyAsync(string key) {
            return Task.FromResult(_apiKeyStore.TryRemove(key, out _));
        }

        public Task<bool> UpdateScopesAsync(string key, List<string> scopes) {
            if (_apiKeyStore.TryGetValue(key, out var entry)) {
                entry.Scopes = scopes;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> ValidateKeyAsync(string key, string? requiredScope = null) {
            if (_apiKeyStore.TryGetValue(key, out var entry)) {
                if (entry.IsRevoked)
                    return Task.FromResult(false);

                if (requiredScope != null && !entry.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase))
                    return Task.FromResult(false);

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<IEnumerable<ApiKey>> GetAllKeysAsync(bool includeRevoked = false) {
            var keys = _apiKeyStore.Values
                .Where(k => includeRevoked || !k.IsRevoked)
                .ToList();

            return Task.FromResult<IEnumerable<ApiKey>>(keys);
        }
    }

}
