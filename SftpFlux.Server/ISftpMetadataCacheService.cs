namespace SftpFlux.Server {
    public interface ISftpMetadataCacheService {
        IEnumerable<SftpMetadataEntry>? GetDirectoryEntries(string path);
        void SetDirectoryEntries(string path, IEnumerable<SftpMetadataEntry> entries);
        void Invalidate(string path); // optional for cache busting
        void Clear(); // clear all cache
    }
}
