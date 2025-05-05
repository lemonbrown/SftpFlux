namespace SftpFlux.Server {
    public class ApiKey {
        public string Key { get; set; } = Guid.NewGuid().ToString("N");
        public List<string> Scopes { get; set; } = new();
        public bool IsRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
    }


}
