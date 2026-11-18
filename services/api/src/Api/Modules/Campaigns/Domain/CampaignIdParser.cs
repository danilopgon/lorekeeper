using Api.SharedKernel;

namespace Api.Modules.Campaigns.Domain;

public static class CampaignIdParser
{
    public const string InvalidCode = "campaign_id_invalid";

    public static DomainResult<Guid> Parse(string? value) =>
        CanonicalUuid.TryParse(value, out var id)
            ? DomainResult<Guid>.Success(id)
            : DomainResult<Guid>.Failure(InvalidCode);

    public static string Format(Guid id) => CanonicalUuid.Format(id);
}
