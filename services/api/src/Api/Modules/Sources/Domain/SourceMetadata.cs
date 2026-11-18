using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

/// <summary>The operator-editable fields of a source, validated together for paste, import and edit.</summary>
public sealed record SourceMetadata(SourceTitle Title, SourceCategory? Category, KnowledgeStatus KnowledgeStatus)
{
    public static Result<SourceMetadata> Parse(string? title, string? category, string? knowledgeStatus)
    {
        var parsedTitle = SourceTitle.Create(title);
        var parsedCategory = SourceClassification.ParseCategory(category);
        var parsedStatus = SourceClassification.ParseKnowledgeStatus(knowledgeStatus);

        IReadOnlyList<FieldError> errors =
        [
            .. parsedTitle.FieldErrors("title"),
            .. parsedCategory.FieldErrors("category"),
            .. parsedStatus.FieldErrors("knowledgeStatus")
        ];

        if (errors.Count > 0)
        {
            return Error.InvalidFields("Source metadata is invalid.", errors);
        }

        return new SourceMetadata(parsedTitle.Value, parsedCategory.Value, parsedStatus.Value);
    }
}
