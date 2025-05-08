namespace SftpFlux.Server.Helpers {
    public static class FileKeyHelper {
        public static string GetFileKey(SftpMetadataEntry entry)
            => $"{entry.Path}/{entry.Name}:{entry.LastModifiedUtc:o}";
    }
}
