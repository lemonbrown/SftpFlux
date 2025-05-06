namespace SftpFlux.Server.Authorization {
    public class RequireScopeFilter : IEndpointFilter {

        private readonly string _requiredScope;

        public RequireScopeFilter(string requiredScope) {
            _requiredScope = requiredScope;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var httpContext = context.HttpContext;
            var apiKeyService = httpContext.RequestServices.GetRequiredService<IApiKeyService>();

            if (!httpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
                return Results.Unauthorized();

            var apiKey = apiKeyHeader.ToString();

            if (!await apiKeyService.ValidateKeyAsync(apiKey))
                return Results.Unauthorized();

            var key = await apiKeyService.GetKeyAsync(apiKey);
            if (key == null || key.IsRevoked)
                return Results.Unauthorized();

            if (!ScopeValidator.IsScopeAllowed(key.Scopes, _requiredScope, ""))
                return Results.Forbid();

            return await next(context);
        }
    }

}
