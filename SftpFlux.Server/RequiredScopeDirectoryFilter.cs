using System.Net.Http.Headers;

namespace SftpFlux.Server {
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
            
            if (!httpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                if (String.IsNullOrWhiteSpace(ReplApiKey.ApiKey))
                    return Results.Unauthorized();
            

            var apiKey = !String.IsNullOrWhiteSpace(apiKeyHeader.ToString()) ? apiKeyHeader.ToString() : ReplApiKey.ApiKey;
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
