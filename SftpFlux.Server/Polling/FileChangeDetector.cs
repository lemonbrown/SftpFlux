namespace SftpFlux.Server.Polling {
    public class FileChangeDetector {
        public static IEnumerable<FileChangeEvent> DetectChanges(
            List<SftpMetadataEntry> oldList,
            List<SftpMetadataEntry> newList,
            string path) {
            var oldDict = oldList.ToDictionary(f => f.Name);
            var newDict = newList.ToDictionary(f => f.Name);

            // Created or modified
            foreach (var newFile in newDict.Values) {
                if (!oldDict.TryGetValue(newFile.Name, out var oldFile)) {
                    yield return new FileChangeEvent { Path = path, FileName = newFile.Name, ChangeType = FileChangeType.Created };
                } else if (newFile.LastModifiedUtc != oldFile.LastModifiedUtc) {
                    yield return new FileChangeEvent { Path = path, FileName = newFile.Name, ChangeType = FileChangeType.Modified };
                }
            }

            // Deleted
            foreach (var oldFile in oldDict.Values) {
                if (!newDict.ContainsKey(oldFile.Name)) {
                    yield return new FileChangeEvent { Path = path, FileName = oldFile.Name, ChangeType = FileChangeType.Deleted };
                }
            }
        }
    }

}
