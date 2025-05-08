namespace SftpFlux.Server.Polling {
    public class FileChangeEvent {
        public string Path { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public FileChangeType ChangeType { get; set; }
        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;

        public string? MetadataJson { get; set; } // optional full metadata
    }
}
