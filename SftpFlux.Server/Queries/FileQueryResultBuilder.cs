using Microsoft.AspNetCore.WebUtilities;

namespace SftpFlux.Server.Queries {
    public static class FileQueryResultBuilder {
        public static FileQueryResult Build(FileQuery query, List<SftpMetadataEntry> items, int totalCount, string baseUrl) {
            var page = query.Page ?? 1;
            var pageSize = query.PageSize ?? 50;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            string BuildUrl(int targetPage) {
                var queryParams = new Dictionary<string, string?> {
                    ["nameContains"] = query.NameContains,
                    ["fileType"] = query.FileType,
                    ["modifiedFrom"] = query.ModifiedFrom?.ToString("o"),
                    ["modifiedTo"] = query.ModifiedTo?.ToString("o"),
                    ["createdFrom"] = query.CreatedFrom?.ToString("o"),
                    ["createdTo"] = query.CreatedTo?.ToString("o"),
                    ["sortBy"] = query.SortBy,
                    ["sortOrder"] = query.SortOrder,
                    ["page"] = targetPage.ToString(),
                    ["pageSize"] = pageSize.ToString()
                };

                var filtered = queryParams
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .ToDictionary(kv => kv.Key, kv => kv.Value!);

                return QueryHelpers.AddQueryString(baseUrl, filtered);
            }

            return new FileQueryResult() {
                Results = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Next = page < totalPages ? BuildUrl(page + 1) : null,
                Previous = page > 1 ? BuildUrl(page - 1) : null
            };
        }
    }

}
