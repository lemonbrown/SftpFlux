namespace SftpFlux.Server.Pipes {
    public interface IPipeCommand {

        string Name { get; }
        Task<string> ExecuteAsync(string[] args);
    }
}
