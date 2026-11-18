using System.Net.Sockets;
using Api.Modules.Sources.Application.ProcessSourceVersion;
using Api.Modules.Sources.Domain;
using MediatR;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Api.Modules.Sources.Infrastructure.Ingestion;

public sealed class SourceIngestionOptions
{
    public const string SectionName = "Sources:Ingestion";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan Lease { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAttempts { get; set; } = IngestionRetryPolicy.Default.MaxAttempts;
    public TimeSpan RetryBaseDelay { get; set; } = IngestionRetryPolicy.Default.BaseDelay;

    public IngestionRetryPolicy RetryPolicy => new(MaxAttempts, RetryBaseDelay);
}

public sealed class SourceIngestionRunner(
    IServiceScopeFactory scopeFactory,
    IOptions<SourceIngestionOptions> options,
    TimeProvider timeProvider,
    ILogger<SourceIngestionRunner> logger)
{
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        ClaimedIngestionJob? job;
        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var queue = claimScope.ServiceProvider.GetRequiredService<IngestionJobQueue>();
            job = await queue.ClaimNextAsync(timeProvider.GetUtcNow(), options.Value.Lease, options.Value.MaxAttempts, cancellationToken);
        }

        if (job is null)
        {
            return false;
        }

        try
        {
            await using var processScope = scopeFactory.CreateAsyncScope();
            var sender = processScope.ServiceProvider.GetRequiredService<ISender>();
            var outcome = (await sender.Send(new ProcessSourceVersionCommand(job), cancellationToken)).Value;
            logger.LogInformation("Ingestion job {JobId} finished as {Outcome} after {Attempts} attempt(s).", job.JobId, outcome, job.Attempts);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var outcome = IsTransient(exception)
                ? options.Value.RetryPolicy.AfterTransientFailure(job.Attempts, timeProvider.GetUtcNow())
                : IngestionRetryPolicy.AfterUnexpectedFailure();

            logger.LogWarning(
                "Ingestion job {JobId} failed on attempt {Attempts} with {ExceptionType}; next state {State} {ErrorCode}.",
                job.JobId,
                job.Attempts,
                exception.GetType().Name,
                outcome.State,
                outcome.ErrorCode);

            await using var failureScope = scopeFactory.CreateAsyncScope();
            var queue = failureScope.ServiceProvider.GetRequiredService<IngestionJobQueue>();
            await queue.RecordFailureAsync(job, outcome, timeProvider.GetUtcNow(), cancellationToken);
        }

        return true;
    }

    public static bool IsTransient(Exception exception) => exception switch
    {
        NpgsqlException { IsTransient: true } => true,
        TimeoutException or SocketException or HttpRequestException => true,
        _ when exception.InnerException is not null => IsTransient(exception.InnerException),
        _ => false
    };
}
