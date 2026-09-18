namespace Api.Modules.Campaigns.Domain;

public sealed class Campaign
{
    private Campaign()
    {
        Name = null!;
    }

    private Campaign(Guid id, CampaignName name, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }
    public CampaignName Name { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Campaign Create(CampaignName name, DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        return new Campaign(Guid.NewGuid(), name, utcNow, utcNow);
    }
}
