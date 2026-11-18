using Api.Modules.Campaigns.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Campaigns.Application.CreateCampaign;

public sealed class CreateCampaignHandler(ICampaignRepository campaigns) : IRequestHandler<CreateCampaignCommand, Result<CampaignDto>>
{
    public static Error NameConflict { get; } = Error.Conflict("campaign_name_conflict", "Campaign name already exists.").ForField("name");

    public async Task<Result<CampaignDto>> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var name = CampaignName.Create(request.Name);
        if (!name.IsSuccess)
        {
            return name.Error!.ForField("name");
        }

        var campaign = Campaign.Create(name.Value, DateTimeOffset.UtcNow);
        campaigns.Add(campaign);

        if (await campaigns.SaveChangesAsync(cancellationToken) == CampaignSaveOutcome.NameConflict)
        {
            return NameConflict;
        }

        return new CampaignDto(
            campaign.Id.ToString("D"),
            campaign.Name.Value,
            campaign.CreatedAt,
            campaign.UpdatedAt);
    }
}

public sealed record CreateCampaignCommand(string? Name) : IRequest<Result<CampaignDto>>;
