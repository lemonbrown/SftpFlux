namespace SftpFlux.Server
{
    public interface ISftpConnectionRegistry
    {
        SftpConnectionInfo? Get(string id);
        IEnumerable<SftpConnectionInfo> GetAll();
        void Add(SftpConnectionInfo connectionInfo);
        void Remove(string id);
    }
}
