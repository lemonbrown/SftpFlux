namespace SftpFlux.Server.Authorization
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public bool IsAdmin { get; set; }
    }

}
