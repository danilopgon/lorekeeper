using Api.Common.Presentation;
using Api.Modules.Sources.Application.FindContentMatches;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/content-matches")]
[Tags("Sources")]
public sealed class ContentMatchesController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "FindContentMatches")]
    [EndpointSummary("Find sources with the same content")]
    [EndpointDescription("Reports which content hashes already belong to a non-removed source of this campaign, for the import review.")]
    [ProducesResponseType(typeof(ContentMatchesDto), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ContentMatchesDto>> FindContentMatches(
        string campaignId,
        [FromBody] ContentMatchesRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new FindContentMatchesQuery(campaignId, request?.Hashes), cancellationToken);

        return result.Match<ActionResult<ContentMatchesDto>>(matches => Ok(matches), error => error.ToProblem());
    }

    public sealed record ContentMatchesRequest(IReadOnlyList<string>? Hashes);
}
