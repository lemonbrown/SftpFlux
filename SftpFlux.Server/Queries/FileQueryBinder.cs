using Microsoft.AspNetCore.WebUtilities;

namespace SftpFlux.Server.Queries {
    public static class FileQueryBinder {
        public static FileQuery FromUrl(string url) {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            // If relative, prepend dummy host to satisfy Uri parser
            if (!uri.IsAbsoluteUri)
                uri = new Uri("http://localhost" + url);

            var query = QueryHelpers.ParseQuery(uri.Query);

            string GetString(string key) => query.TryGetValue(key, out var v) ? v.ToString() : null;
            DateTimeOffset? GetDate(string key) => DateTimeOffset.TryParse(GetString(key), out var dt) ? dt : null;
            int? GetInt(string key) => int.TryParse(GetString(key), out var i) ? i : null;

            return new FileQuery(
                NameContains: GetString("nameContains"),
                FileType: GetString("fileType"),
                ModifiedFrom: GetDate("modifiedFrom"),
                ModifiedTo: GetDate("modifiedTo"),
                CreatedFrom: GetDate("createdFrom"),
                CreatedTo: GetDate("createdTo"),
                SortBy: GetString("sortBy"),
                SortOrder: GetString("sortOrder"),
                Path: uri.AbsolutePath ?? ".",
                Page: GetInt("page") ?? 1,
                PageSize: GetInt("pageSize") ?? 50
            );
        }
    }

}
