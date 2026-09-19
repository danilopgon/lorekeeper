namespace Api.Modules.Campaigns.Application;

public sealed record CampaignDto(string Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
