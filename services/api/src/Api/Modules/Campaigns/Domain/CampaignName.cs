namespace Api.Modules.Campaigns.Domain;

public sealed record CampaignName
{
    public const int MaxLength = 120;
    public const string InvalidCode = "campaign_name_invalid";

    private CampaignName(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<CampaignName> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            return DomainResult<CampaignName>.Failure(InvalidCode);
        }

        return DomainResult<CampaignName>.Success(new CampaignName(trimmed));
    }
}
