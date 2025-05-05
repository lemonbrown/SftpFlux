namespace SftpFlux.Server {
    public static class ScopeValidator {
        public static bool IsScopeAllowed(IEnumerable<string> scopes, string requiredAction, string requiredPath) {
            foreach (var scope in scopes) {
                var parts = scope.Split(':', 2);
                if (parts.Length != 2)
                    continue;

                var action = parts[0];
                var path = parts[1];

                if (!string.Equals(action, requiredAction, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Allow if requiredPath starts with the scoped path
                if (requiredPath.StartsWith(path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

}
