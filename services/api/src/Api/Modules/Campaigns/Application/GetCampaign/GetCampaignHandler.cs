using Api.Modules.Campaigns.Domain;
using Api.Modules.Campaigns.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Application.GetCampaign;

public sealed class GetCampaignHandler(CampaignsDbContext dbContext) : IRequestHandler<GetCampaignQuery, GetCampaignResult>
{
    public async Task<GetCampaignResult> Handle(GetCampaignQuery request, CancellationToken cancellationToken)
    {
        var parsed = CampaignIdParser.Parse(request.CampaignId);
        if (!parsed.IsSuccess)
        {
            return GetCampaignResult.InvalidId();
        }

        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == parsed.Value, cancellationToken);

        return campaign is null
            ? GetCampaignResult.NotFound()
            : GetCampaignResult.Found(new CampaignDto(
                campaign.Id.ToString("D"),
                campaign.Name.Value,
                campaign.CreatedAt,
                campaign.UpdatedAt));
    }
}

public sealed record GetCampaignQuery(string CampaignId) : IRequest<GetCampaignResult>;

public sealed record GetCampaignResult(GetCampaignStatus Status, CampaignDto? Campaign)
{
    public static GetCampaignResult Found(CampaignDto campaign) => new(GetCampaignStatus.Found, campaign);

    public static GetCampaignResult InvalidId() => new(GetCampaignStatus.InvalidId, null);

    public static GetCampaignResult NotFound() => new(GetCampaignStatus.NotFound, null);
}

public enum GetCampaignStatus
{
    Found,
    InvalidId,
    NotFound
}
