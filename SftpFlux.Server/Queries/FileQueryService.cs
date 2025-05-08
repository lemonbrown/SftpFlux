using Renci.SshNet;
using SftpFlux.Server.Caching;
using SftpFlux.Server.Connection;

namespace SftpFlux.Server.Queries {

    public class FileQueryService : IFileQueryService {
        private readonly ISftpConnectionRegistry _connectionRegistry;
        private readonly ISftpMetadataCacheService _cacheService;

        public FileQueryService(
            ISftpConnectionRegistry connectionRegistry,
            ISftpMetadataCacheService cacheService) {
            _connectionRegistry = connectionRegistry;
            _cacheService = cacheService;
        }

        public async Task<FileQueryResult> QueryFilesAsync(FileQuery query) {
            var connection = _connectionRegistry.GetAll().First(); // Simplified: always use the first one
            var path = query.Path ?? ".";

            var cached = _cacheService.GetDirectoryEntries(path, connection.Id);

            List<SftpMetadataEntry> entries;

            if (cached != null) {
                entries = cached.ToList();
            } else {
                var client = ConnectSftp(connection);

                if (!client.Exists(path))
                    return new(); // Or throw NotFoundException

                entries = client.ListDirectory(path)
                    .Where(f => f.Name != "." && f.Name != "..")
                    .Select(f => new SftpMetadataEntry {
                        Path = path,
                        Name = f.Name,
                        IsDirectory = f.IsDirectory,
                        Size = f.Attributes.Size,
                        LastModifiedUtc = f.Attributes.LastWriteTimeUtc,
                        Url = "http://localhost:5000/file/" + (path != "." ? path : "") + f.Name
                    })
                    .ToList();

                _cacheService.SetDirectoryEntries(path, entries, connection.Id);
            }

            var originalTotalCount = entries.Count;

            // Apply filters
            entries = entries
                .Where(e =>
                    (string.IsNullOrEmpty(query.NameContains) || e.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(query.FileType) || e.Name.EndsWith(query.FileType, StringComparison.OrdinalIgnoreCase)) &&
                    (!query.ModifiedFrom.HasValue || e.LastModifiedUtc >= query.ModifiedFrom.Value) &&
                    (!query.ModifiedTo.HasValue || e.LastModifiedUtc <= query.ModifiedTo.Value))
                .ToList();

            // Apply sort (e.g., by LastModified descending)
            entries = entries
                .OrderByDescending(e => e.LastModifiedUtc)
                .ToList();

            // Apply paging
            if (query.Page.HasValue && query.PageSize.HasValue) {
                var skip = (query.Page.Value - 1) * query.PageSize.Value;
                entries = entries.Skip(skip).Take(query.PageSize.Value).ToList();
            }

            FileQueryResult fileQueryResult = FileQueryResultBuilder.Build(query, entries, originalTotalCount, "http://localhost:5000/files");

            return fileQueryResult;
        }

        private SftpClient ConnectSftp(SftpConnectionInfo connectionInfo) {
            var client = new SftpClient(connectionInfo.Host, connectionInfo.Port, connectionInfo.Username, connectionInfo.Password);
            client.Connect();
            return client;
        }
    }

}
