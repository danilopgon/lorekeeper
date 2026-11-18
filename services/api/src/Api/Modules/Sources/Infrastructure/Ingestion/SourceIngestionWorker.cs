using Microsoft.Extensions.Options;

namespace Api.Modules.Sources.Infrastructure.Ingestion;

public sealed class SourceIngestionWorker(
    SourceIngestionRunner runner,
    IOptions<SourceIngestionOptions> options,
    TimeProvider timeProvider,
    ILogger<SourceIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool processed;
            try
            {
                processed = await runner.RunNextAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The ingestion worker could not claim or record a job.");
                processed = false;
            }

            if (!processed)
            {
                try
                {
                    await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
