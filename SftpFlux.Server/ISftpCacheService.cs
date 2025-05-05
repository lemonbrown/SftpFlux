using Renci.SshNet.Sftp;

namespace SftpFlux.Server
{
    public interface ISftpCacheService
    {
        Task<IEnumerable<CachedFileMetadata>> GetDirectoryMetadataAsync(string path);
        Task<Stream?> GetFileContentAsync(string fullPath);
        Task CacheFileAsync(SftpFile file, Stream content);
        Task SaveMetadataAsync();
    }
}
