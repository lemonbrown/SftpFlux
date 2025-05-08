namespace SftpFlux.Server.Pipes {

    public class AddSftpCommand : IPipeCommand {

        public string Name => "add sftp";

        public Task<string> ExecuteAsync(string[] args) {
            // Use real logic here
            string name = args.ElementAtOrDefault(0) ?? "unknown";
            return Task.FromResult($"Added SFTP config: {name}");
        }
    }
}
