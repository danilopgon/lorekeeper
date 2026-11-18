using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.PasteSource;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/pastes")]
[Tags("Sources")]
public sealed class PasteSourceController(PasteSourceHandler pasteSource) : ControllerBase
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
        var result = await pasteSource.Handle(
            new PasteSourceCommand(campaignId, request?.Title, request?.Text, request?.Category, request?.KnowledgeStatus),
            cancellationToken);

        return result.Status switch
        {
            PasteSourceStatus.Accepted => Accepted(result.Source),
            PasteSourceStatus.Invalid => SourceProblemDetails.Invalid(result.Errors),
            PasteSourceStatus.CampaignIdInvalid => SourceProblemDetails.CampaignIdInvalid(),
            PasteSourceStatus.CampaignNotFound => SourceProblemDetails.CampaignNotFound(),
            _ => throw new InvalidOperationException($"Unexpected paste source status '{result.Status}'.")
        };
    }

    public sealed record PasteSourceRequest(string? Title, string? Text)
    {
        public string? Category { get; init; }

        public string? KnowledgeStatus { get; init; }
    }
}
