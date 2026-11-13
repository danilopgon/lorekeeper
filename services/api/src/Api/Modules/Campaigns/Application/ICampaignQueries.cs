namespace Api.Modules.Campaigns.Application;

public interface ICampaignQueries
{
    Task<CampaignDto?> GetByIdAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken cancellationToken);
}
