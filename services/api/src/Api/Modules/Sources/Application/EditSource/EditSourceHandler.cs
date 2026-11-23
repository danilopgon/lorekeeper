using Api.Modules.Campaigns.Application;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.EditSource;

public sealed class EditSourceHandler(
    ICampaignQueries campaigns,
    ISourceRepository sources,
    ISourceQueries sourceQueries,
    TimeProvider timeProvider) : IRequestHandler<EditSourceCommand, Result<SourceDto>>
{
    public async Task<Result<SourceDto>> Handle(EditSourceCommand command, CancellationToken cancellationToken)
    {
        var key = await SourceRoute.ResolveAsync(campaigns, command.CampaignId, command.SourceId, cancellationToken);
        if (!key.IsSuccess)
        {
            return key.Error!;
        }

        var title = command.Title is { } titleEdit ? SourceTitle.Create(titleEdit.Value) : null;
        var category = command.Category is { } categoryEdit ? SourceClassification.ParseCategory(categoryEdit.Value) : null;
        var knowledgeStatus = command.KnowledgeStatus is { } statusEdit ? ParseRequiredKnowledgeStatus(statusEdit.Value) : null;

        IReadOnlyList<FieldError> errors =
        [
            .. title?.FieldErrors("title") ?? [],
            .. category?.FieldErrors("category") ?? [],
            .. knowledgeStatus?.FieldErrors("knowledgeStatus") ?? []
        ];
        if (errors.Count > 0)
        {
            return Error.InvalidFields("Source metadata is invalid.", errors);
        }

        var locked = await sources.LockAsync(key.Value.CampaignId, key.Value.SourceId, cancellationToken);
        if (locked is null)
        {
            return SourceIdParser.NotFound;
        }

        var source = locked.Source;
        var edited = new SourceMetadata(
            title?.Value ?? source.Title,
            category is null ? source.Category : category.Value,
            knowledgeStatus?.Value ?? source.KnowledgeStatus);

        if (source.Edit(edited, timeProvider.GetUtcNow()))
        {
            await sources.SaveChangesAsync(cancellationToken);
        }

        return await sourceQueries.GetAsync(key.Value.CampaignId, key.Value.SourceId, cancellationToken)
            ?? throw new InvalidOperationException("An edited source must be readable.");
    }

    /// <summary>Unlike a new source, where an absent status means `unknown`, an explicit null in an edit is invalid (`06`).</summary>
    private static Result<KnowledgeStatus> ParseRequiredKnowledgeStatus(string? value) =>
        value is null
            ? Error.Validation(SourceClassification.KnowledgeStatusInvalidCode, "Source knowledge status is invalid.")
            : SourceClassification.ParseKnowledgeStatus(value);
}

/// <summary>A field the request contains, possibly with a null value; an absent field is a null <see cref="FieldEdit"/>.</summary>
public sealed record FieldEdit(string? Value);

public sealed record EditSourceCommand(
    string CampaignId,
    string SourceId,
    FieldEdit? Title,
    FieldEdit? Category,
    FieldEdit? KnowledgeStatus) : IRequest<Result<SourceDto>>;
