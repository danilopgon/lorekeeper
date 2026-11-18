using Api.Common.Presentation;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.PasteSource;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/pastes")]
[Tags("Sources")]
public sealed class PasteSourceController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "PasteSource")]
    [EndpointSummary("Paste text as a source")]
    [EndpointDescription("Creates a source from pasted text and queues it for processing.")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SourceDto>> PasteSource(
        string campaignId,
        [FromBody] PasteSourceRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PasteSourceCommand(campaignId, request?.Title, request?.Text, request?.Category, request?.KnowledgeStatus),
            cancellationToken);

        return result.Match<ActionResult<SourceDto>>(source => Accepted(source), error => error.ToProblem());
    }

    public sealed record PasteSourceRequest(string? Title, string? Text)
    {
        public string? Category { get; init; }

        public string? KnowledgeStatus { get; init; }
    }
}
