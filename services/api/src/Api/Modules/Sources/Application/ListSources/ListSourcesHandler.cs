using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.ListSources;

public sealed class ListSourcesHandler(ICampaignQueries campaigns, ISourceQueries sources)
    : IRequestHandler<ListSourcesQuery, Result<IReadOnlyList<SourceDto>>>
{
    public async Task<Result<IReadOnlyList<SourceDto>>> Handle(ListSourcesQuery query, CancellationToken cancellationToken)
    {
        var campaignId = CampaignIdParser.Parse(query.CampaignId);
        if (!campaignId.IsSuccess)
        {
            return campaignId.Error!;
        }

        if (await campaigns.GetByIdAsync(campaignId.Value, cancellationToken) is null)
        {
            return CampaignIdParser.NotFound;
        }

        return Result<IReadOnlyList<SourceDto>>.Success(await sources.ListAsync(campaignId.Value, cancellationToken));
    }
}

public sealed record ListSourcesQuery(string CampaignId) : IRequest<Result<IReadOnlyList<SourceDto>>>;
