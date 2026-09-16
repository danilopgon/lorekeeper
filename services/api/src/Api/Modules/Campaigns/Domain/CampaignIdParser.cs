using System.Text.RegularExpressions;

namespace Api.Modules.Campaigns.Domain;

public static partial class CampaignIdParser
{
    public const string InvalidCode = "campaign_id_invalid";

    public static DomainResult<Guid> Parse(string? value)
    {
        if (value is null || !CanonicalUuidRegex().IsMatch(value) || !Guid.TryParse(value, out var id))
        {
            return DomainResult<Guid>.Failure(InvalidCode);
        }

        return DomainResult<Guid>.Success(id);
    }

    public static string Format(Guid id) => id.ToString("D").ToLowerInvariant();

    [GeneratedRegex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex CanonicalUuidRegex();
}
