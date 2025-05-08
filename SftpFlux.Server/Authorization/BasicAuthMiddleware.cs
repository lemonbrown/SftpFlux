using SftpFlux.Server.User;
using System.Security.Claims;
using System.Text;

namespace SftpFlux.Server.Authorization {
    public class BasicAuthMiddleware {
        private readonly RequestDelegate _next;

        public BasicAuthMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserService userService) {

            if (!context.Request.Path.StartsWithSegments("/admin"))
                await _next(context);           

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader?.StartsWith("Basic ") == true) {
                var encoded = authHeader.Substring("Basic ".Length).Trim();
                var credentialBytes = Convert.FromBase64String(encoded);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

                if (credentials.Length == 2 &&
                    await userService.ValidateCredentialsAsync(credentials[0], credentials[1])) {
                    // Optional: set identity
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                    new Claim(ClaimTypes.Name, credentials[0]),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "Basic"));

                    await _next(context);
                    return;
                }
            }

            context.Response.StatusCode = 401;
            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"SftpFlux\"";
            await context.Response.WriteAsync("Unauthorized");
        }
    }

}
