using Api.SharedKernel;

namespace Api.Modules.Campaigns.Domain;

public static class CampaignIdParser
{
    public const string InvalidCode = "campaign_id_invalid";
    public const string NotFoundCode = "campaign_not_found";

    public static Error NotFound { get; } = Error.NotFound(NotFoundCode, "Campaign was not found.");

    public static Result<Guid> Parse(string? value) =>
        CanonicalUuid.TryParse(value, out var id)
            ? id
            : Error.Validation(InvalidCode, "Campaign id is invalid.");

    public static string Format(Guid id) => CanonicalUuid.Format(id);
}
