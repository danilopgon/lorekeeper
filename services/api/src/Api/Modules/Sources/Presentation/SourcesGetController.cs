using Api.Common.Presentation;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.ListSources;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources")]
[Tags("Sources")]
public sealed class SourcesGetController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "ListSources")]
    [EndpointSummary("List sources")]
    [EndpointDescription("Returns the campaign's non-removed sources, newest first.")]
    [ProducesResponseType(typeof(IReadOnlyList<SourceDto>), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<SourceDto>>> ListSources(string campaignId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListSourcesQuery(campaignId), cancellationToken);

        return result.Match<ActionResult<IReadOnlyList<SourceDto>>>(sources => Ok(sources), error => error.ToProblem());
    }
}
