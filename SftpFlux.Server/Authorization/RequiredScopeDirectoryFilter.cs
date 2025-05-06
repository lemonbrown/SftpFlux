using System.Net.Http.Headers;

namespace SftpFlux.Server.Authorization {
    public class RequireScopedDirectoryFilter : IEndpointFilter {

        private readonly string _requiredAction;
        private readonly Func<EndpointFilterInvocationContext, string> _pathAccessor;

        public RequireScopedDirectoryFilter(string requiredAction, Func<EndpointFilterInvocationContext, string> pathAccessor) {
            _requiredAction = requiredAction;
            _pathAccessor = pathAccessor;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var httpContext = context.HttpContext;
            var apiKeyService = httpContext.RequestServices.GetRequiredService<IApiKeyService>();

            if (GlobalConfig.IsTestBypassAuth)
                return await next(context);

            if (!httpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                return Results.Unauthorized();


            var apiKey = apiKeyHeader.ToString();

            if (!await apiKeyService.ValidateKeyAsync(apiKey))
                return Results.Unauthorized();

            var key = await apiKeyService.GetKeyAsync(apiKey);
            if (key == null || key.IsRevoked)
                return Results.Unauthorized();

            var targetPath = _pathAccessor(context);

            if (key.Scopes != null && key.Scopes.Any() && !ScopeValidator.IsScopeAllowed(key.Scopes, _requiredAction, targetPath))
                return Results.Unauthorized();

            return await next(context);
        }
    }

}
