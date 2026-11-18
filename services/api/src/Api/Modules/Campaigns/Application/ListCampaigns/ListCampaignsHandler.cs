using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Campaigns.Application.ListCampaigns;

public sealed class ListCampaignsHandler(ICampaignQueries campaigns) : IRequestHandler<ListCampaignsQuery, Result<IReadOnlyList<CampaignDto>>>
{
    public async Task<Result<IReadOnlyList<CampaignDto>>> Handle(ListCampaignsQuery request, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<CampaignDto>>.Success(await campaigns.ListAsync(cancellationToken));
}

public sealed record ListCampaignsQuery : IRequest<Result<IReadOnlyList<CampaignDto>>>;
