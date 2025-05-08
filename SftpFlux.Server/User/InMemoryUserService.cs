using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text;

namespace SftpFlux.Server.User {

    public class InMemoryUserService : IUserService {

        private readonly List<User> _users = new();

        public InMemoryUserService() {
            // Add a default admin
            _users.Add(new User {
                Username = "foo",
                Password = "bar",
                IsAdmin = true
            });
        }

        public Task<bool> ValidateCredentialsAsync(string username, string password) {
            var user = _users.FirstOrDefault(u => u.Username == username);
            return Task.FromResult(user != null && password ==  user.Password);
        }

        public async Task<bool> CheckForAdminInHeaders(HttpContext context) {

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader?.StartsWith("Basic ") == true) {
                var encoded = authHeader.Substring("Basic ".Length).Trim();
                var credentialBytes = Convert.FromBase64String(encoded);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

                if (credentials.Length == 2 &&
                    await ValidateCredentialsAsync(credentials[0], credentials[1])) {

                    return true;
                }
            }

            return false;

        }
    }

}
