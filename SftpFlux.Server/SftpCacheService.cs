using Renci.SshNet.Sftp;
using System.Text.Json;

namespace SftpFlux.Server
{
    public class SftpCacheService : ISftpCacheService
    {
        private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "cache");
        private readonly string _metadataFile;
        private Dictionary<string, List<CachedFileMetadata>> _metadataCache = new();

        public SftpCacheService()
        {
            _metadataFile = Path.Combine(_cacheDir, "metadata.json");
            Directory.CreateDirectory(Path.Combine(_cacheDir, "files"));
            LoadMetadata();
        }

        public Task<IEnumerable<CachedFileMetadata>> GetDirectoryMetadataAsync(string path)
        {
            if (_metadataCache.TryGetValue(path, out var list))
                return Task.FromResult<IEnumerable<CachedFileMetadata>>(list);
            return Task.FromResult<IEnumerable<CachedFileMetadata>>(Array.Empty<CachedFileMetadata>());
        }

        public Task<Stream?> GetFileContentAsync(string fullPath)
        {
            string filePath = GetCachedFilePath(fullPath);
            return File.Exists(filePath)
                ? Task.FromResult<Stream?>(File.OpenRead(filePath))
                : Task.FromResult<Stream?>(null);
        }

        public async Task CacheFileAsync(SftpFile file, Stream content)
        {
            string dir = Path.GetDirectoryName(file.FullName)!;
            string filePath = GetCachedFilePath(file.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var fs = File.Create(filePath))
            {
                await content.CopyToAsync(fs);
            }

            var metadata = new CachedFileMetadata(
                file.Name,
                file.FullName,
                file.Attributes.Size,
                file.Attributes.LastWriteTimeUtc,
                "TODO: hash" // Add hash if needed
            );

            if (!_metadataCache.ContainsKey(dir))
                _metadataCache[dir] = new List<CachedFileMetadata>();

            _metadataCache[dir].RemoveAll(f => f.FullPath == file.FullName);
            _metadataCache[dir].Add(metadata);
        }

        public Task SaveMetadataAsync()
        {
            var json = JsonSerializer.Serialize(_metadataCache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_metadataFile, json);
            return Task.CompletedTask;
        }

        private void LoadMetadata()
        {
            if (File.Exists(_metadataFile))
            {
                var json = File.ReadAllText(_metadataFile);
                _metadataCache = JsonSerializer.Deserialize<Dictionary<string, List<CachedFileMetadata>>>(json)
                                  ?? new();
            }
        }

        private string GetCachedFilePath(string fullPath)
        {
            var relativePath = fullPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            return Path.Combine(_cacheDir, "files", relativePath);
        }
    }

}
