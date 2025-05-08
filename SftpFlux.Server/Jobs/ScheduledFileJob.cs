using SftpFlux.Server.Queries;

namespace SftpFlux.Server.Jobs {

    public class ScheduledFileJob {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Cron { get; set; } = "* * * * *";
        public FileQuery Query { get; set; }
        public Uri Url { get; set; } = default!;

        // Tracking
        public DateTimeOffset? LastRunUtc { get; set; }
        public HashSet<string> ProcessedFileKeys { get; set; } = new();
    }

}
