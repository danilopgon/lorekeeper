using Api.Common.Presentation;
using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Campaigns.Presentation;

[ApiController]
[Route("api/campaigns/")]
[Tags("Campaigns")]
public sealed class CreateCampaignController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "CreateCampaign")]
    [EndpointSummary("Create a campaign")]
    [EndpointDescription("Creates one campaign and returns its canonical campaign representation.")]
    [ProducesResponseType(typeof(CampaignDto), StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignCommand(request?.Name), cancellationToken);

        return result.Match<ActionResult<CampaignDto>>(
            campaign => Created($"/api/campaigns/{campaign.Id}", campaign),
            error => error.ToProblem());
    }

    public sealed record CreateCampaignRequest(string? Name);
}
