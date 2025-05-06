using LiteDB;

namespace SftpFlux.Server.Authorization {

    public class ApiKey {

        [BsonId]
        public string Key { get; set; } = Guid.NewGuid().ToString("N");

        public List<string> Scopes { get; set; } = [];

        public bool IsRevoked { get; set; } = false;

        public List<string> SftpIds { get; set; } = [];

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public List<string> WebhookAllowedPaths { get; set; } = [];
    }


}
