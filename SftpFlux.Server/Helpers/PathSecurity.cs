using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace SftpFlux.Server.Helpers {

    public static class PathSecurity {

        public static bool IsPathAllowed(string requestedPath, List<string> allowedPatterns)
            => allowedPatterns.Any(pattern => Regex.IsMatch(requestedPath, pattern));

        public static bool AreAllRequestedPathsAllowed(List<string> includePaths, List<string> allowedPatterns)
            => includePaths.All(path => IsPathAllowed(path, allowedPatterns));
    }
}
