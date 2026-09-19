using Api.Modules.Campaigns.Domain;
using Api.Modules.Campaigns.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Application.CreateCampaign;

public sealed class CreateCampaignHandler(CampaignsDbContext dbContext)
{
    public async Task<CreateCampaignResult> Handle(CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var name = CampaignName.Create(request.Name);
        if (!name.IsSuccess)
        {
            return CreateCampaignResult.InvalidName();
        }

        var campaign = Campaign.Create(name.Value!, DateTimeOffset.UtcNow);
        dbContext.Campaigns.Add(campaign);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (CampaignPersistenceErrors.From(exception) == CampaignPersistenceError.NameConflict)
        {
            return CreateCampaignResult.NameConflict();
        }

        return CreateCampaignResult.Created(new CampaignDto(
            campaign.Id.ToString("D"),
            campaign.Name.Value,
            campaign.CreatedAt,
            campaign.UpdatedAt));
    }
}

public sealed record CreateCampaignRequest(string? Name);

public sealed record CreateCampaignResult(CreateCampaignStatus Status, CampaignDto? Campaign)
{
    public static CreateCampaignResult Created(CampaignDto campaign) => new(CreateCampaignStatus.Created, campaign);

    public static CreateCampaignResult InvalidName() => new(CreateCampaignStatus.InvalidName, null);

    public static CreateCampaignResult NameConflict() => new(CreateCampaignStatus.NameConflict, null);
}

public enum CreateCampaignStatus
{
    Created,
    InvalidName,
    NameConflict
}
