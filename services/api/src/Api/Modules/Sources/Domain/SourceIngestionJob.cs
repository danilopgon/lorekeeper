namespace Api.Modules.Sources.Domain;

public enum IngestionJobState
{
    Queued,
    Processing,
    Succeeded,
    Failed
}

public sealed class SourceIngestionJob
{
    public const string UnavailableCode = "source_processing_unavailable";
    public const string FailedCode = "source_processing_failed";

    private SourceIngestionJob()
    {
    }

    private SourceIngestionJob(Guid id, SourceVersion version, DateTimeOffset now)
    {
        Id = id;
        CampaignId = version.CampaignId;
        SourceVersionId = version.Id;
        State = IngestionJobState.Queued;
        NextAttemptAt = now;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid SourceVersionId { get; private set; }
    public IngestionJobState State { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? LeaseExpiresAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static SourceIngestionJob Create(SourceVersion version, DateTimeOffset now) =>
        new(Guid.NewGuid(), version, now.ToUniversalTime());

    /// <summary>Queued or processing: the source cannot take another update until it finishes or fails.</summary>
    public bool IsActive => State is IngestionJobState.Queued or IngestionJobState.Processing;

    public void Succeed(DateTimeOffset now)
    {
        State = IngestionJobState.Succeeded;
        LeaseExpiresAt = null;
        ErrorCode = null;
        UpdatedAt = now.ToUniversalTime();
    }

    /// <summary>A manual retry queues the same version again with a fresh set of automatic attempts.</summary>
    public void Requeue(DateTimeOffset now)
    {
        if (State != IngestionJobState.Failed)
        {
            throw new InvalidOperationException("Only a failed job can be retried.");
        }

        var utcNow = now.ToUniversalTime();
        State = IngestionJobState.Queued;
        Attempts = 0;
        NextAttemptAt = utcNow;
        LeaseExpiresAt = null;
        ErrorCode = null;
        UpdatedAt = utcNow;
    }
}
