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

    public sealed record CreateCampaignRequest(string? Name);
}
