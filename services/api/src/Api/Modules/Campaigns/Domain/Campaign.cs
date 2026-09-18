namespace Api.Modules.Campaigns.Domain;

public sealed class Campaign
{
    private Campaign(Guid id, CampaignName name, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public CampaignName Name { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public static Campaign Create(CampaignName name, DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        return new Campaign(Guid.NewGuid(), name, utcNow, utcNow);
    }
}
