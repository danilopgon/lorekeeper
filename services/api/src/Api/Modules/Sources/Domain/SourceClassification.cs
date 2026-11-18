using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

public enum SourceOrigin
{
    File,
    Paste
}

public enum SourceCategory
{
    SessionNotes,
    Preparation,
    CharactersPlaces,
    GeneralReference
}

public enum KnowledgeStatus
{
    Unknown,
    Preparation,
    Occurred
}

public static class SourceClassification
{
    public const string CategoryInvalidCode = "source_category_invalid";
    public const string KnowledgeStatusInvalidCode = "source_knowledge_status_invalid";

    private static readonly Dictionary<string, SourceCategory> Categories = new(StringComparer.Ordinal)
    {
        ["session_notes"] = SourceCategory.SessionNotes,
        ["preparation"] = SourceCategory.Preparation,
        ["characters_places"] = SourceCategory.CharactersPlaces,
        ["general_reference"] = SourceCategory.GeneralReference
    };

    private static readonly Dictionary<string, KnowledgeStatus> KnowledgeStatuses = new(StringComparer.Ordinal)
    {
        ["unknown"] = KnowledgeStatus.Unknown,
        ["preparation"] = KnowledgeStatus.Preparation,
        ["occurred"] = KnowledgeStatus.Occurred
    };

    public static Result<SourceCategory?> ParseCategory(string? value)
    {
        if (value is null)
        {
            return Result<SourceCategory?>.Success(null);
        }

        return Categories.TryGetValue(value, out var category)
            ? Result<SourceCategory?>.Success(category)
            : Error.Validation(CategoryInvalidCode, "Source category is invalid.");
    }

    public static Result<KnowledgeStatus> ParseKnowledgeStatus(string? value)
    {
        if (value is null)
        {
            return KnowledgeStatus.Unknown;
        }

        return KnowledgeStatuses.TryGetValue(value, out var status)
            ? Result<KnowledgeStatus>.Success(status)
            : Error.Validation(KnowledgeStatusInvalidCode, "Source knowledge status is invalid.");
    }
}
