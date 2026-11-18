using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Infrastructure.Persistence;

public sealed class CampaignRepository(CampaignsDbContext dbContext) : ICampaignRepository
{
    public void Add(Campaign campaign) => dbContext.Campaigns.Add(campaign);

    public async Task<CampaignSaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return CampaignSaveOutcome.Saved;
        }
        catch (DbUpdateException exception) when (CampaignPersistenceErrors.From(exception) == CampaignPersistenceError.NameConflict)
        {
            return CampaignSaveOutcome.NameConflict;
        }
    }
}
