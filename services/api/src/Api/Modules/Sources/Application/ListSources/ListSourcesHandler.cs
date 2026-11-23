using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.ListSources;

public sealed class ListSourcesHandler(SourceLookup lookup, ISourceQueries sources)
    : IRequestHandler<ListSourcesQuery, Result<IReadOnlyList<SourceDto>>>
{
    public async Task<Result<IReadOnlyList<SourceDto>>> Handle(ListSourcesQuery query, CancellationToken cancellationToken)
    {
        var campaignId = await lookup.FindCampaignAsync(query.CampaignId, cancellationToken);
        if (!campaignId.IsSuccess)
        {
            return campaignId.Error!;
        }

        return Result<IReadOnlyList<SourceDto>>.Success(await sources.ListAsync(campaignId.Value, cancellationToken));
    }
}

public sealed record ListSourcesQuery(string CampaignId) : IRequest<Result<IReadOnlyList<SourceDto>>>;
