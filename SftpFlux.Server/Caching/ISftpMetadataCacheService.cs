namespace SftpFlux.Server.Caching {
    public interface ISftpMetadataCacheService {
        IEnumerable<SftpMetadataEntry>? GetDirectoryEntries(string path, string? sftpId);
        void SetDirectoryEntries(string path, IEnumerable<SftpMetadataEntry> entries, string? sftpId);
        void Invalidate(string path); // optional for cache busting
        void Clear(); // clear all cache
    }
}
