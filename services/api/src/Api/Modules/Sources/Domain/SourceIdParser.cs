using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

public static class SourceIdParser
{
    public const string InvalidCode = "source_id_invalid";
    public const string NotFoundCode = "source_not_found";

    /// <summary>Also the answer for a removed source and for a source of another campaign.</summary>
    public static Error NotFound { get; } = Error.NotFound(NotFoundCode, "Source was not found.");

    public static Result<Guid> Parse(string? value) =>
        CanonicalUuid.TryParse(value, out var id)
            ? id
            : Error.Validation(InvalidCode, "Source id is invalid.");
}
