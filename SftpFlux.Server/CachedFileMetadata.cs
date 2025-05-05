namespace SftpFlux.Server
{
    public record CachedFileMetadata(
       string Name,
       string FullPath,
       long Size,
       DateTime LastModified,
       string Hash
   );

}
