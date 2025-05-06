namespace SftpFlux.Server.Connection {
    public class SftpConnectionInfoRequest {

        public string Id { get; set; } // e.g. "vendorA", "client1"
        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; } // Or use a secret store later
        public string? BasePath { get; set; }
    }
}
