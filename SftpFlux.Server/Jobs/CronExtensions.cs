namespace SftpFlux.Server.Jobs {
    public static class CronExtensions {
        public static bool IsTimeDue(this Cronos.CronExpression cron, DateTimeOffset now, DateTimeOffset? lastRunUtc) {
            var next = cron.GetNextOccurrence(lastRunUtc ?? now.AddMinutes(-1), TimeZoneInfo.Utc);
            return next.HasValue && next <= now;
        }
    }
}
