using Api.Common.Presentation;
using Api.Modules.Sources.Application.RemoveSource;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/{sourceId}")]
[Tags("Sources")]
public sealed class RemoveSourceController(ISender sender) : ControllerBase
{
    [HttpDelete(Name = "RemoveSource")]
    [EndpointSummary("Remove a source")]
    [EndpointDescription("Removes the source and its versions, even while one is processing; only a tombstone remains.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RemoveSource(string campaignId, string sourceId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveSourceCommand(campaignId, sourceId), cancellationToken);

        return result.Match<IActionResult>(_ => NoContent(), error => error.ToProblem());
    }
}
