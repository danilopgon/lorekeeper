using Api.Modules.Campaigns.Domain;
using MediatR;

namespace Api.Modules.Campaigns.Application.CreateCampaign;

public sealed class CreateCampaignHandler(ICampaignRepository campaigns) : IRequestHandler<CreateCampaignCommand, CreateCampaignResult>
{
    public async Task<CreateCampaignResult> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var name = CampaignName.Create(request.Name);
        if (!name.IsSuccess)
        {
            return CreateCampaignResult.InvalidName();
        }

        var campaign = Campaign.Create(name.Value!, DateTimeOffset.UtcNow);
        campaigns.Add(campaign);

        var saved = await campaigns.SaveChangesAsync(cancellationToken);
        if (saved == CampaignSaveResult.NameConflict)
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

public sealed record CreateCampaignCommand(string? Name) : IRequest<CreateCampaignResult>;

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
