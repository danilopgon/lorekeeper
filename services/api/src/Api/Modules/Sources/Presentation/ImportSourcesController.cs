using System.Text.Json;
using Api.Common.Presentation;
using Api.Modules.Sources.Application.ImportSources;
using Api.Modules.Sources.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/imports")]
[Tags("Sources")]
public sealed class ImportSourcesController(ISender sender) : ControllerBase
{
    /// <summary>52 MiB: room for 50 files of 1 MiB plus multipart overhead (`06`).</summary>
    public const long MaxRequestBytes = 52L * 1024 * 1024;

    [HttpPost(Name = "ImportSources")]
    [EndpointSummary("Import Markdown and text files as sources")]
    [EndpointDescription("Validates each file independently and queues every accepted one for processing.")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [RejectOversizedForm]
    [ProducesResponseType(typeof(ImportSourcesResultDto), StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    public async Task<ActionResult<ImportSourcesResultDto>> ImportSources(
        string campaignId,
        [FromForm] ImportSourcesForm form,
        CancellationToken cancellationToken)
    {
        var files = new List<ImportFile>();
        foreach (var file in form.Files ?? [])
        {
            files.Add(await ReadAsync(file, cancellationToken));
        }

        var result = await sender.Send(new ImportSourcesCommand(campaignId, files, ParseMetadata(form.Metadata)), cancellationToken);

        return result.Match<ActionResult<ImportSourcesResultDto>>(imported => Accepted(imported), error => error.ToProblem());
    }

    private static async Task<ImportFile> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > SourceFormats.MaxFileBytes)
        {
            return new ImportFile(file.FileName, file.Length, Content: null);
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        return new ImportFile(file.FileName, file.Length, buffer.ToArray());
    }

    /// <summary>Anything that is not a JSON array of entries becomes <c>null</c>, which the request validator rejects.</summary>
    private static IReadOnlyList<ImportMetadataEntry>? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<ImportMetadataEntry>>(metadata, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public sealed class ImportSourcesForm
    {
        /// <summary>1–50 `.md`, `.markdown` or `.txt` files.</summary>
        [FromForm(Name = "files")]
        public List<IFormFile>? Files { get; init; }

        /// <summary>A JSON array with one `{ "title", "category", "knowledgeStatus" }` entry per file, in the same order.</summary>
        [FromForm(Name = "metadata")]
        public string? Metadata { get; init; }
    }
}
