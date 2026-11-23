using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;

namespace Api.Modules.Sources.Application;

public sealed record SourceKey(Guid CampaignId, Guid SourceId);

/// <summary>Resolves `/campaigns/{campaignId}/sources/{sourceId}`: campaign errors come first, as on every Sources route (`06`).</summary>
public static class SourceRoute
{
    public static async Task<Result<SourceKey>> ResolveAsync(
        ICampaignQueries campaigns,
        string campaignId,
        string sourceId,
        CancellationToken cancellationToken)
    {
        var campaign = CampaignIdParser.Parse(campaignId);
        if (!campaign.IsSuccess)
        {
            return campaign.Error!;
        }

        if (await campaigns.GetByIdAsync(campaign.Value, cancellationToken) is null)
        {
            return CampaignIdParser.NotFound;
        }

        var source = SourceIdParser.Parse(sourceId);
        return source.IsSuccess ? new SourceKey(campaign.Value, source.Value) : source.Error!;
    }

    /// <summary>
    /// Reads the response after the source lock is released, so a removal that commits in between makes it
    /// <c>source_not_found</c> rather than an unexpected error.
    /// </summary>
    public static async Task<Result<SourceDto>> ReadBackAsync(this ISourceQueries queries, SourceKey key, CancellationToken cancellationToken) =>
        await queries.GetAsync(key.CampaignId, key.SourceId, cancellationToken) is { } source
            ? source
            : SourceIdParser.NotFound;
}
