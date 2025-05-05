namespace SftpFlux.Server {
    public class InMemorySftpMetadataCacheService : ISftpMetadataCacheService {
        private class CacheEntry {
            public List<SftpMetadataEntry> Entries { get; set; } = default!;
            public DateTime CachedAtUtc { get; set; }
        }

        private readonly Dictionary<string, CacheEntry> _cache = new();
        private readonly object _lock = new();
        private readonly TimeSpan _ttl;

        public InMemorySftpMetadataCacheService(TimeSpan? ttl = null) {
            _ttl = ttl ?? TimeSpan.FromMinutes(5); // Default TTL
        }

        public IEnumerable<SftpMetadataEntry>? GetDirectoryEntries(string path) {
            lock (_lock) {
                if (_cache.TryGetValue(path, out var entry)) {
                    if (DateTime.UtcNow - entry.CachedAtUtc < _ttl) {
                        return entry.Entries;
                    } else {
                        _cache.Remove(path); // Expired
                    }
                }
                return null;
            }
        }

        public void SetDirectoryEntries(string path, IEnumerable<SftpMetadataEntry> entries) {
            lock (_lock) {
                _cache[path] = new CacheEntry {
                    Entries = entries.ToList(),
                    CachedAtUtc = DateTime.UtcNow
                };
            }
        }

        public void Invalidate(string path) {
            lock (_lock) {
                _cache.Remove(path);
            }
        }

        public void Clear() {
            lock (_lock) {
                _cache.Clear();
            }
        }
    }

}
