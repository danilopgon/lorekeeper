using Api.SharedKernel;

namespace Api.Modules.Campaigns.Domain;

public sealed record CampaignName
{
    public const int MaxLength = 120;
    private const char NullCharacter = (char)0;
    public const string InvalidCode = "campaign_name_invalid";

    private CampaignName(string value) => Value = value;

    public string Value { get; }

    public static Result<CampaignName> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || UnicodeScalars.Count(trimmed) > MaxLength || trimmed.Contains(NullCharacter))
        {
            return Error.Validation(InvalidCode, "Campaign name is invalid.");
        }

        return new CampaignName(trimmed);
    }
}
