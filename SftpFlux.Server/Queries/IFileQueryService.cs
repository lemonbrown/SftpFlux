namespace SftpFlux.Server.Queries {
    public interface IFileQueryService {
        Task<FileQueryResult> QueryFilesAsync(FileQuery query);
    }

}
