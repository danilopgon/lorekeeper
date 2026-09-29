using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Campaigns.Presentation;

[ApiController]
[Route("api/campaigns/")]
[Tags("Campaigns")]
public sealed class CampaignsController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "ListCampaigns")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CampaignDto>>> ListCampaigns(CancellationToken cancellationToken)
    {
        var campaigns = await sender.Send(new ListCampaignsQuery(), cancellationToken);
        return Ok(campaigns);
    }

    [HttpPost(Name = "CreateCampaign")]
    [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignCommand(request?.Name), cancellationToken);

        return result.Status switch
        {
            CreateCampaignStatus.Created => Created($"/api/campaigns/{result.Campaign!.Id}", result.Campaign),
            CreateCampaignStatus.InvalidName => CampaignProblemDetails.InvalidName(),
            CreateCampaignStatus.NameConflict => CampaignProblemDetails.NameConflict(),
            _ => throw new InvalidOperationException($"Unexpected create campaign status '{result.Status}'.")
        };
    }

    [HttpGet("{campaignId}", Name = "GetCampaign")]
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

    public sealed record CreateCampaignRequest(string? Name);
}
