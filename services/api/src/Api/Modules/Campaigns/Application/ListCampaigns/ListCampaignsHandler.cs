using Api.Modules.Campaigns.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Application.ListCampaigns;

public sealed class ListCampaignsHandler(CampaignsDbContext dbContext) : IRequestHandler<ListCampaignsQuery, IReadOnlyList<CampaignDto>>
{
    public async Task<IReadOnlyList<CampaignDto>> Handle(ListCampaignsQuery request, CancellationToken cancellationToken)
    {
        var campaigns = await dbContext.Campaigns
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return campaigns
            .OrderBy(campaign => campaign.CreatedAt)
            .ThenBy(campaign => campaign.Name.Value, StringComparer.Ordinal)
            .Select(campaign => new CampaignDto(
                campaign.Id.ToString("D"),
                campaign.Name.Value,
                campaign.CreatedAt,
                campaign.UpdatedAt))
            .ToList();
    }
}

public sealed record ListCampaignsQuery : IRequest<IReadOnlyList<CampaignDto>>;
