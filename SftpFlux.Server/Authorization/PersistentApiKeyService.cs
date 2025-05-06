using LiteDB;
using SftpFlux.Server.Persistence;

namespace SftpFlux.Server.Authorization {


    public class PersistentApiKeyService : IApiKeyService {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<ApiKey> _collection;

        public PersistentApiKeyService(string databasePath = "apikeys.db") {
            _db = new LiteDatabase(databasePath);
            _collection = _db.GetCollection<ApiKey>("apikeys");
            _collection.EnsureIndex(x => x.Key, true);
        }

        public Task<string> CreateKeyAsync(List<string>? scopes, List<string>? sftpIds) {
            var entry = new ApiKey {
                Scopes = scopes ?? [],
                SftpIds = sftpIds ?? [],
                CreatedAt = DateTime.UtcNow
            };

            _collection.Insert(entry);
            return Task.FromResult(entry.Key);
        }

        public Task<bool> RevokeKeyAsync(string key) {
            var entry = _collection.FindOne(x => x.Key == key);
            if (entry is null)
                return Task.FromResult(false);

            entry.IsRevoked = true;
            entry.RevokedAt = DateTime.UtcNow;
            _collection.Update(entry);
            return Task.FromResult(true);
        }

        public Task<bool> ReinstateKeyAsync(string key) {
            var entry = _collection.FindOne(x => x.Key == key);
            if (entry is null)
                return Task.FromResult(false);

            entry.IsRevoked = false;
            entry.RevokedAt = null;
            _collection.Update(entry);
            return Task.FromResult(true);
        }

        public Task<ApiKey?> GetKeyAsync(string key) {
            var entry = _collection.FindOne(x => x.Key == key);
            return Task.FromResult(entry);
        }

        public Task<bool> DeleteKeyAsync(string key) {
            return Task.FromResult(_collection.DeleteMany(x => x.Key == key) > 0);
        }

        public Task<bool> UpdateScopesAsync(string key, List<string> scopes) {
            var entry = _collection.FindOne(x => x.Key == key);
            if (entry is null)
                return Task.FromResult(false);

            entry.Scopes = scopes;
            _collection.Update(entry);
            return Task.FromResult(true);
        }

        public Task<bool> ValidateKeyAsync(string key, string? requiredScope = null) {
            var entry = _collection.FindOne(x => x.Key == key);
            if (entry is null || entry.IsRevoked)
                return Task.FromResult(false);

            if (requiredScope != null && !entry.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase))
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        public Task<IEnumerable<ApiKey>> GetAllKeysAsync(bool includeRevoked = false) {
            var results = _collection.Find(x => includeRevoked || !x.IsRevoked);
            return Task.FromResult(results.AsEnumerable());
        }
    }

}
