using Api.Common.Presentation;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.UpdateSourceContent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/{sourceId}/content")]
[Tags("Sources")]
public sealed class UpdateSourceContentController(ISender sender) : ControllerBase
{
    /// <summary>2 MiB: one file or text of 1 MiB plus multipart overhead (`06`).</summary>
    public const long MaxRequestBytes = 2L * 1024 * 1024;

    private const string FileField = "file";
    private const string TextField = "text";

    [HttpPut(Name = "UpdateSourceContent")]
    [EndpointSummary("Update a source's content")]
    [EndpointDescription("Queues new content as a pending version; the published version stays available until it is replaced.")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [RejectOversizedForm]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    public async Task<ActionResult<SourceDto>> UpdateSourceContent(
        string campaignId,
        string sourceId,
        [FromForm] UpdateSourceContentForm form, // Describes the parts for OpenAPI; the action reads the raw form.
        CancellationToken cancellationToken)
    {
        var (files, texts) = await ReadContentPartsAsync(cancellationToken);
        var result = await sender.Send(new UpdateSourceContentCommand(campaignId, sourceId, files, texts), cancellationToken);

        return result.Match<ActionResult<SourceDto>>(
            update => update.Queued ? Accepted(update.Source) : Ok(update.Source),
            error => error.ToProblem());
    }

    /// <summary>
    /// Every `file` and `text` part of the raw form: model binding would join repeated text fields, keep only the
    /// first file and turn an empty text field into null.
    /// </summary>
    private async Task<(IReadOnlyList<UploadedFile> Files, IReadOnlyList<string> Texts)> ReadContentPartsAsync(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
        {
            return ([], []);
        }

        var files = await UploadedFiles.ReadAllAsync(Request.Form.Files.GetFiles(FileField), cancellationToken);
        return (files, [.. Request.Form[TextField].Select(text => text ?? string.Empty)]);
    }

    public sealed class UpdateSourceContentForm
    {
        /// <summary>A `.md`, `.markdown` or `.txt` file. Send this or `text`, not both.</summary>
        [FromForm(Name = FileField)]
        public IFormFile? File { get; init; }

        /// <summary>The new text. Send this or `file`, not both.</summary>
        [FromForm(Name = TextField)]
        public string? Text { get; init; }
    }
}
