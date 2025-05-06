using SftpFlux.Server.Authorization;

namespace SftpFlux.Server {
    public static class EndpointRouteBuilderExtensions {
        public static RouteHandlerBuilder RequireScopedDirectory(
            this RouteHandlerBuilder builder,
            string action,
            Func<EndpointFilterInvocationContext, string> pathAccessor) {
            return builder.AddEndpointFilter(new RequireScopedDirectoryFilter(action, pathAccessor));
        }

        public static RouteHandlerBuilder RequireScope(this RouteHandlerBuilder builder, string requiredScope) {
            return builder.AddEndpointFilter(new RequireScopeFilter(requiredScope));
        }
    }

}
