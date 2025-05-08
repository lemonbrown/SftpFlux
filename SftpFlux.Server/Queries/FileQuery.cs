namespace SftpFlux.Server.Queries {

    public record FileQuery(
        string? NameContains,
        string? FileType,
        DateTimeOffset? ModifiedFrom,
        DateTimeOffset? ModifiedTo,
        DateTimeOffset? CreatedFrom,
        DateTimeOffset? CreatedTo,
        string? SortBy,
        string? SortOrder,
        string Path = ".",
        int? Page = 1,
        int? PageSize = 50);
}
