namespace SftpFlux.Server.Authorization {
    public class ApiKeyMiddleware {
        private readonly RequestDelegate _next;
        private const string ApiKeyHeader = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IApiKeyService apiKeyService) {
            if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedKey) ||
                !await apiKeyService.ValidateKeyAsync(extractedKey!)) {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or missing API Key");
                return;
            }

            await _next(context);
        }
    }

}
