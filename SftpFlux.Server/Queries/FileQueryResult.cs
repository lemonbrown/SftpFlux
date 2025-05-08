namespace SftpFlux.Server.Queries {
    public class FileQueryResult {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Next { get; set; } = default!;
        public string Previous { get; set; } = default!;
        public List<SftpMetadataEntry> Results { get; set; } = new();
    }
}
