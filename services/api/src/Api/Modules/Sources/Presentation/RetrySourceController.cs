using Api.Common.Presentation;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.RetrySource;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/{sourceId}/retry")]
[Tags("Sources")]
public sealed class RetrySourceController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "RetrySource")]
    [EndpointSummary("Retry a failed source version")]
    [EndpointDescription("Queues the failed pending version again with its attempt count restarted.")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<SourceDto>> RetrySource(string campaignId, string sourceId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RetrySourceCommand(campaignId, sourceId), cancellationToken);

        return result.Match<ActionResult<SourceDto>>(source => Accepted(source), error => error.ToProblem());
    }
}
