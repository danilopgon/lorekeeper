using Api.Modules.Campaigns.Domain;

namespace Api.Modules.Campaigns.Application;

public interface ICampaignRepository
{
    void Add(Campaign campaign);

    Task<CampaignSaveOutcome> SaveChangesAsync(CancellationToken cancellationToken);
}

public enum CampaignSaveOutcome
{
    Saved,
    NameConflict
}
