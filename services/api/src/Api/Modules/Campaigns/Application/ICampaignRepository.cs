using Api.Modules.Campaigns.Domain;

namespace Api.Modules.Campaigns.Application;

public interface ICampaignRepository
{
    void Add(Campaign campaign);

    Task<CampaignSaveResult> SaveChangesAsync(CancellationToken cancellationToken);
}

public enum CampaignSaveResult
{
    Saved,
    NameConflict
}
