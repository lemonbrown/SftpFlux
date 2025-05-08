namespace SftpFlux.Server.Pipes {
    public class CommandDispatcher {
        private readonly Dictionary<string, IPipeCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

        public CommandDispatcher(IEnumerable<IPipeCommand> commands) {
            foreach (var cmd in commands) {
                _commands[cmd.Name.ToLower()] = cmd;
            }
        }

        public async Task<string> DispatchAsync(string input) {
            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = tokens.Length; i > 0; i--) {
                var possibleCmd = string.Join(' ', tokens.Take(i)).ToLower();
                if (_commands.TryGetValue(possibleCmd, out var cmd)) {
                    var args = tokens.Skip(i).ToArray();
                    return await cmd.ExecuteAsync(args);
                }
            }

            return "Unknown command";
        }
    }


}
