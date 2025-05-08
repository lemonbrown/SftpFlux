using Microsoft.Extensions.Logging;
using SftpFlux.Server.Helpers;
using SftpFlux.Server.Queries;

namespace SftpFlux.Server.Jobs {
    public class ScheduledJobProcessor(
        IScheduledJobRegistry jobRegistry,
        IFileQueryService queryService,
        IHttpClientFactory httpFactory,
        ILogger<ScheduledJobProcessor> logger) {

        public async Task RunDueJobsAsync() {

            var now = DateTimeOffset.UtcNow;

            foreach (var job in jobRegistry.GetAllJobs()) {
                if (!Cronos.CronExpression.Parse(job.Cron).IsTimeDue(now, job.LastRunUtc))
                    continue;

                logger.LogInformation("Running job {JobId}", job.Id);

                var query = job.Query with {
                    ModifiedFrom = job.LastRunUtc
                };

                var results = await queryService.QueryFilesAsync(query);

                var newFiles = results.Results
                    .Where(entry => !job.ProcessedFileKeys.Contains(FileKeyHelper.GetFileKey(entry)))
                    .ToList();

                results.Results = newFiles;

                if (newFiles.Any()) {
                    var client = httpFactory.CreateClient();
                    var response = await client.PostAsJsonAsync(job.Url, results);

                    if (response.IsSuccessStatusCode) {
                        foreach (var file in newFiles)
                            job.ProcessedFileKeys.Add(FileKeyHelper.GetFileKey(file));

                        logger.LogInformation("Job {JobId} sent {Count} new files", job.Id, newFiles.Count);
                    } else {
                        logger.LogWarning("Job {JobId} webhook failed with status {StatusCode}", job.Id, response.StatusCode);
                    }
                }

                job.LastRunUtc = now;
            }
        }
    }

}
