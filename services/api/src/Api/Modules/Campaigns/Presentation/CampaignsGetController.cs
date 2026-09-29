using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Campaigns.Presentation;

[ApiController]
[Route("api/campaigns/")]
[Tags("Campaigns")]
public sealed class CampaignsGetController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "ListCampaigns")]
    [EndpointSummary("List campaigns")]
    [EndpointDescription("Returns the campaigns available to the single operator.")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CampaignDto>>> ListCampaigns(CancellationToken cancellationToken)
    {
        var campaigns = await sender.Send(new ListCampaignsQuery(), cancellationToken);
        return Ok(campaigns);
    }

    [HttpGet("{campaignId}", Name = "GetCampaign")]
    [EndpointSummary("Get a campaign")]
    [EndpointDescription("Returns one campaign by canonical UUID.")]
    [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampaignDto>> GetCampaign(string campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignQuery(campaignId), cancellationToken);

        return result.Status switch
        {
            GetCampaignStatus.Found => Ok(result.Campaign),
            GetCampaignStatus.InvalidId => CampaignProblemDetails.InvalidId(),
            GetCampaignStatus.NotFound => CampaignProblemDetails.NotFound(),
            _ => throw new InvalidOperationException($"Unexpected get campaign status '{result.Status}'.")
        };
    }
}
