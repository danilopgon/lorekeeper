using System.Text.Json.Serialization;
using Api.Common.Presentation;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.EditSource;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

[ApiController]
[Route("api/campaigns/{campaignId}/sources/{sourceId}")]
[Tags("Sources")]
public sealed class EditSourceController(ISender sender) : ControllerBase
{
    [HttpPatch(Name = "EditSource")]
    [EndpointSummary("Edit a source's metadata")]
    [EndpointDescription("Changes any subset of title, category and knowledge status without creating a version; absent fields stay as they are.")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SourceDto>> EditSource(
        string campaignId,
        string sourceId,
        [FromBody] EditSourceRequest? request,
        CancellationToken cancellationToken)
    {
        var edit = request ?? new EditSourceRequest();
        var result = await sender.Send(
            new EditSourceCommand(
                campaignId,
                sourceId,
                edit.HasTitle ? new FieldEdit(edit.Title) : null,
                edit.HasCategory ? new FieldEdit(edit.Category) : null,
                edit.HasKnowledgeStatus ? new FieldEdit(edit.KnowledgeStatus) : null),
            cancellationToken);

        return result.Match<ActionResult<SourceDto>>(source => Ok(source), error => error.ToProblem());
    }

    /// <summary>The JSON reader calls a setter only for a property the body contains, even when its value is null.</summary>
    public sealed class EditSourceRequest
    {
        private readonly string? _title;
        private readonly string? _category;
        private readonly string? _knowledgeStatus;

        public string? Title
        {
            get => _title;
            init
            {
                _title = value;
                HasTitle = true;
            }
        }

        /// <summary>`null` clears the category.</summary>
        public string? Category
        {
            get => _category;
            init
            {
                _category = value;
                HasCategory = true;
            }
        }

        public string? KnowledgeStatus
        {
            get => _knowledgeStatus;
            init
            {
                _knowledgeStatus = value;
                HasKnowledgeStatus = true;
            }
        }

        [JsonIgnore]
        public bool HasTitle { get; private init; }

        [JsonIgnore]
        public bool HasCategory { get; private init; }

        [JsonIgnore]
        public bool HasKnowledgeStatus { get; private init; }
    }
}
