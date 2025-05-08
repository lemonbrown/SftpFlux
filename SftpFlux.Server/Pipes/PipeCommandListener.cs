using System.IO.Pipes;
using System.Text;

namespace SftpFlux.Server.Pipes {

    public class PipeCommandListener {
        private readonly CancellationToken _cancellationToken;

        public PipeCommandListener(CancellationToken cancellationToken) {
            _cancellationToken = cancellationToken;
        }

        public void Start() {
            Task.Run(async () => {
                while (!_cancellationToken.IsCancellationRequested) {
                    try {
                        using var pipeServer = new NamedPipeServerStream(
                             "sftpflux-admin",
                             PipeDirection.InOut,
                             1,
                             PipeTransmissionMode.Message,
                             PipeOptions.Asynchronous);

                        Console.WriteLine("[Server] Waiting for client...");
                        await pipeServer.WaitForConnectionAsync(_cancellationToken);
                        Console.WriteLine("[Server] Client connected.");

                        byte[] buffer = new byte[1024];
                        int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, _cancellationToken);

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[Server] Received: {message}");

                        var responseBytes = Encoding.UTF8.GetBytes("ACK: " + message);
                        await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, _cancellationToken);
                    } catch (IOException ex) {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
        }
    }
}
