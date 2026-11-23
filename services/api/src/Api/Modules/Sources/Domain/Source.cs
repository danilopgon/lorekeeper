namespace Api.Modules.Sources.Domain;

public sealed class Source
{
    private Source()
    {
        Title = null!;
    }

    private Source(
        Guid id,
        Guid campaignId,
        SourceTitle title,
        SourceOrigin origin,
        SourceCategory? category,
        KnowledgeStatus knowledgeStatus,
        DateTimeOffset now)
    {
        Id = id;
        CampaignId = campaignId;
        Title = title;
        Origin = origin;
        Category = category;
        KnowledgeStatus = knowledgeStatus;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public SourceTitle Title { get; private set; }
    public SourceOrigin Origin { get; private set; }
    public SourceCategory? Category { get; private set; }
    public KnowledgeStatus KnowledgeStatus { get; private set; }
    public Guid? PublishedVersionId { get; private set; }
    public DateTimeOffset? LastPublishedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsRemoved => RemovedAt is not null;

    public static Source Create(Guid campaignId, SourceMetadata metadata, SourceOrigin origin, DateTimeOffset now) =>
        new(Guid.NewGuid(), campaignId, metadata.Title, origin, metadata.Category, metadata.KnowledgeStatus, now.ToUniversalTime());

    public void Publish(SourceVersion version, DateTimeOffset now)
    {
        if (version.SourceId != Id || version.CampaignId != CampaignId)
        {
            throw new InvalidOperationException("A source can only publish one of its own versions.");
        }

        if (IsRemoved)
        {
            throw new InvalidOperationException("A removed source cannot publish a version.");
        }

        var utcNow = now.ToUniversalTime();
        PublishedVersionId = version.Id;
        LastPublishedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Applies edited metadata; returns false, leaving <see cref="UpdatedAt"/> untouched, when nothing differs.</summary>
    public bool Edit(SourceMetadata metadata, DateTimeOffset now)
    {
        EnsureNotRemoved();

        if (Title == metadata.Title && Category == metadata.Category && KnowledgeStatus == metadata.KnowledgeStatus)
        {
            return false;
        }

        Title = metadata.Title;
        Category = metadata.Category;
        KnowledgeStatus = metadata.KnowledgeStatus;
        UpdatedAt = now.ToUniversalTime();
        return true;
    }

    public void ReceiveContentUpdate(DateTimeOffset now)
    {
        EnsureNotRemoved();
        UpdatedAt = now.ToUniversalTime();
    }

    public void Remove(DateTimeOffset now)
    {
        EnsureNotRemoved();

        var utcNow = now.ToUniversalTime();
        RemovedAt = utcNow;
        PublishedVersionId = null;
        UpdatedAt = utcNow;
    }

    private void EnsureNotRemoved()
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException("A removed source cannot change.");
        }
    }
}
