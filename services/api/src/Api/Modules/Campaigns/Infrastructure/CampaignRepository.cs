using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Infrastructure;

public sealed class CampaignRepository(CampaignsDbContext dbContext) : ICampaignRepository
{
    public void Add(Campaign campaign) => dbContext.Campaigns.Add(campaign);

    public async Task<CampaignSaveResult> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return CampaignSaveResult.Saved;
        }
        catch (DbUpdateException exception) when (CampaignPersistenceErrors.From(exception) == CampaignPersistenceError.NameConflict)
        {
            return CampaignSaveResult.NameConflict;
        }
    }
}
