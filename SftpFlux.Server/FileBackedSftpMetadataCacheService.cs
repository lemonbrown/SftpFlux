using System.Text.Json;

namespace SftpFlux.Server {
    public class FileBackedSftpMetadataCacheService : ISftpMetadataCacheService {
        private readonly string _cacheFilePath;
        private readonly TimeSpan _ttl;
        private readonly Dictionary<string, (List<SftpMetadataEntry> Entries, DateTime CachedAtUtc)> _cache = new();
        private readonly object _lock = new();

        public FileBackedSftpMetadataCacheService(string cacheFilePath, TimeSpan? ttl = null) {
            _cacheFilePath = cacheFilePath;
            _ttl = ttl ?? TimeSpan.FromMinutes(5);

            if (File.Exists(_cacheFilePath)) {
                try {
                    var content = File.ReadAllText(_cacheFilePath);
                    var stored = JsonSerializer.Deserialize<Dictionary<string, CachedData>>(content);
                    if (stored != null) {
                        foreach (var (key, value) in stored) {
                            _cache[key] = (value.Entries, value.CachedAtUtc);
                        }
                    }
                } catch {
                    // Ignore deserialization errors
                }
            }
        }

        public IEnumerable<SftpMetadataEntry>? GetDirectoryEntries(string path) {
            lock (_lock) {
                if (_cache.TryGetValue(path, out var data) &&
                    DateTime.UtcNow - data.CachedAtUtc < _ttl) {
                    return data.Entries;
                } else {
                    _cache.Remove(path);
                    return null;
                }
            }
        }

        public void SetDirectoryEntries(string path, IEnumerable<SftpMetadataEntry> entries) {
            lock (_lock) {
                _cache[path] = (entries.ToList(), DateTime.UtcNow);
                SaveToFile();
            }
        }

        public void Invalidate(string path) {
            lock (_lock) {
                _cache.Remove(path);
                SaveToFile();
            }
        }

        public void Clear() {
            lock (_lock) {
                _cache.Clear();
                SaveToFile();
            }
        }

        private void SaveToFile() {
            var serializable = _cache.ToDictionary(
                kv => kv.Key,
                kv => new CachedData { Entries = kv.Value.Entries, CachedAtUtc = kv.Value.CachedAtUtc });

            File.WriteAllText(_cacheFilePath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        private class CachedData {
            public List<SftpMetadataEntry> Entries { get; set; } = default!;
            public DateTime CachedAtUtc { get; set; }
        }
    }

}
