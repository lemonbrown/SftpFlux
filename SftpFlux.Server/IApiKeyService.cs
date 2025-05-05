namespace SftpFlux.Server {
    public interface IApiKeyService {

        public Task<string> CreateKeyAsync(List<string> scopes);

        public Task<bool> RevokeKeyAsync(string key);

        public Task<bool> ReinstateKeyAsync(string key);

        public Task<ApiKey?> GetKeyAsync(string key);

        public Task<bool> DeleteKeyAsync(string key);

        public Task<bool> UpdateScopesAsync(string key, List<string> scopes);

        public Task<bool> ValidateKeyAsync(string key, string? requiredScope = null);

        public Task<IEnumerable<ApiKey>> GetAllKeysAsync(bool includeRevoked = false);
    }
}
