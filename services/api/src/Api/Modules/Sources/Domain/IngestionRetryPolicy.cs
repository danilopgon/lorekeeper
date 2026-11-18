namespace Api.Modules.Sources.Domain;

public sealed record IngestionRetryPolicy(int MaxAttempts, TimeSpan BaseDelay)
{
    public static IngestionRetryPolicy Default { get; } = new(3, TimeSpan.FromSeconds(2));

    public IngestionFailureOutcome AfterTransientFailure(int attempts, DateTimeOffset now) =>
        attempts >= MaxAttempts
            ? IngestionFailureOutcome.Fail(SourceIngestionJob.UnavailableCode)
            : IngestionFailureOutcome.Retry(now + BaseDelay * Math.Pow(2, attempts - 1));

    public static IngestionFailureOutcome AfterUnexpectedFailure() =>
        IngestionFailureOutcome.Fail(SourceIngestionJob.FailedCode);
}

public sealed record IngestionFailureOutcome(IngestionJobState State, DateTimeOffset? NextAttemptAt, string? ErrorCode)
{
    public static IngestionFailureOutcome Retry(DateTimeOffset nextAttemptAt) => new(IngestionJobState.Queued, nextAttemptAt, null);

    public static IngestionFailureOutcome Fail(string errorCode) => new(IngestionJobState.Failed, null, errorCode);
}
