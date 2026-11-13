using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Infrastructure;

public sealed class CampaignQueries(CampaignsDbContext dbContext) : ICampaignQueries
{
    public async Task<CampaignDto?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == campaignId)
            .Select(campaign => new CampaignRow(campaign.Id, campaign.Name, campaign.CreatedAt, campaign.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return row?.ToDto();
    }

    public async Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Campaigns
            .AsNoTracking()
            .Select(campaign => new CampaignRow(campaign.Id, campaign.Name, campaign.CreatedAt, campaign.UpdatedAt))
            .ToListAsync(cancellationToken);

        // Ordinal name ordering is applied in memory: PostgreSQL collation is not ordinal.
        return rows
            .OrderBy(row => row.CreatedAt)
            .ThenBy(row => row.Name.Value, StringComparer.Ordinal)
            .Select(row => row.ToDto())
            .ToList();
    }

    private sealed record CampaignRow(Guid Id, CampaignName Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
    {
        public CampaignDto ToDto() => new(Id.ToString("D"), Name.Value, CreatedAt, UpdatedAt);
    }
}
