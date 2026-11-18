using Api.Modules.Campaigns.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Campaigns.Application.GetCampaign;

public sealed class GetCampaignHandler(ICampaignQueries campaigns) : IRequestHandler<GetCampaignQuery, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> Handle(GetCampaignQuery request, CancellationToken cancellationToken)
    {
        var campaignId = CampaignIdParser.Parse(request.CampaignId);
        if (!campaignId.IsSuccess)
        {
            return campaignId.Error!;
        }

        var campaign = await campaigns.GetByIdAsync(campaignId.Value, cancellationToken);

        if (campaign is null)
        {
            return CampaignIdParser.NotFound;
        }

        return campaign;
    }
}

public sealed record GetCampaignQuery(string CampaignId) : IRequest<Result<CampaignDto>>;
