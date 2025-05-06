namespace SftpFlux.Server
{
    public class InMemorySftpConnectionRegistry : ISftpConnectionRegistry
    {
        private readonly Dictionary<string, SftpConnectionInfo> _connections = new();

        public SftpConnectionInfo? Get(string id) => _connections.TryGetValue(id, out var conn) ? conn : null;

        public IEnumerable<SftpConnectionInfo> GetAll() => _connections.Values;

        public void Add(SftpConnectionInfo connectionInfo) => _connections[connectionInfo.Id] = connectionInfo;

        public void Remove(string id) => _connections.Remove(id);
    }
}
