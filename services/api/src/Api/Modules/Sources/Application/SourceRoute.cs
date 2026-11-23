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
}
