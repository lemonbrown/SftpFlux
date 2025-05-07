using System.IO.Pipes;
using System.Text;

namespace SftpFlux.Server
{

    public class PipeCommandListener
    {
        private readonly CancellationToken _cancellationToken;

        public PipeCommandListener(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var pipeServer = new NamedPipeServerStream("sftpflux-admin", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                        Console.WriteLine("[Pipe] Waiting for command...");

                        await pipeServer.WaitForConnectionAsync(_cancellationToken);

                        Console.WriteLine("Server: Client connected!");

                        using (var reader = new StreamReader(pipeServer, Encoding.UTF8, leaveOpen: true))
                        using (var writer = new StreamWriter(pipeServer, Encoding.UTF8, leaveOpen: true) { AutoFlush = true })
                        {
                            Console.WriteLine("[Server] Waiting for line...");
                            string? command = await reader.ReadLineAsync();  // Wait for data from client

                            if (command != null)
                            {
                                Console.WriteLine($"[Server] Received: {command}");
                                // After reading, we can write back
                                await writer.WriteLineAsync("ACK: " + command);  // Send acknowledgment
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
        }
    }
}
