namespace SftpFlux.Server.Connection {
    public interface ISftpConnectionRegistry {
        SftpConnectionInfo? Get(string id);
        IEnumerable<SftpConnectionInfo> GetAll();
        void Add(SftpConnectionInfo connectionInfo);
        void Remove(string id);
    }
}
