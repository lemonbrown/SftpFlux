namespace SftpFlux.Server.User {

    public interface IUserService {

        Task<bool> ValidateCredentialsAsync(string username, string password);

        public Task<bool> CheckForAdminInHeaders(HttpContext context);
    }

}
