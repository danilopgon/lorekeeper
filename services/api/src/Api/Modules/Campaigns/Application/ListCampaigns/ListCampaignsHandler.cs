using MediatR;

namespace Api.Modules.Campaigns.Application.ListCampaigns;

public sealed class ListCampaignsHandler(ICampaignQueries campaigns) : IRequestHandler<ListCampaignsQuery, IReadOnlyList<CampaignDto>>
{
    public Task<IReadOnlyList<CampaignDto>> Handle(ListCampaignsQuery request, CancellationToken cancellationToken) =>
        campaigns.ListAsync(cancellationToken);
}

public sealed record ListCampaignsQuery : IRequest<IReadOnlyList<CampaignDto>>;
