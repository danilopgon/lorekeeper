using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.ListSources;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources")]
[Tags("Sources")]
public sealed class SourcesGetController(ListSourcesHandler listSources) : ControllerBase
{
    [HttpGet(Name = "ListSources")]
    [EndpointSummary("List sources")]
    [EndpointDescription("Returns the campaign's non-removed sources, newest first.")]
    [ProducesResponseType(typeof(IReadOnlyList<SourceDto>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<SourceDto>>> ListSources(string campaignId, CancellationToken cancellationToken)
    {
        var result = await listSources.Handle(new ListSourcesQuery(campaignId), cancellationToken);

        return result.Status switch
        {
            ListSourcesStatus.Found => Ok(result.Sources),
            ListSourcesStatus.CampaignIdInvalid => SourceProblemDetails.CampaignIdInvalid(),
            ListSourcesStatus.CampaignNotFound => SourceProblemDetails.CampaignNotFound(),
            _ => throw new InvalidOperationException($"Unexpected list sources status '{result.Status}'.")
        };
    }
}
