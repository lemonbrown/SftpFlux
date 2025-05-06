namespace SftpFlux.Server {
    public class SftpMetadataEntry {
        public string Path { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string FullPath => System.IO.Path.Combine(Path, Name);
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public string SftpId { get; set; } = default!;
        public DateTime LastModifiedUtc { get; set; }
    }

}
