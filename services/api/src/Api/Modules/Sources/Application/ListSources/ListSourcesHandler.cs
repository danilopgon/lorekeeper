using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;

namespace Api.Modules.Sources.Application.ListSources;

public sealed class ListSourcesHandler(ICampaignQueries campaigns, ISourceQueries sources)
{
    public async Task<ListSourcesResult> Handle(ListSourcesQuery query, CancellationToken cancellationToken)
    {
        var campaignId = CampaignIdParser.Parse(query.CampaignId);
        if (!campaignId.IsSuccess)
        {
            return new(ListSourcesStatus.CampaignIdInvalid, []);
        }

        if (await campaigns.GetByIdAsync(campaignId.Value, cancellationToken) is null)
        {
            return new(ListSourcesStatus.CampaignNotFound, []);
        }

        return new(ListSourcesStatus.Found, await sources.ListAsync(campaignId.Value, cancellationToken));
    }
}

public sealed record ListSourcesQuery(string CampaignId);

public sealed record ListSourcesResult(ListSourcesStatus Status, IReadOnlyList<SourceDto> Sources);

public enum ListSourcesStatus
{
    Found,
    CampaignIdInvalid,
    CampaignNotFound
}
