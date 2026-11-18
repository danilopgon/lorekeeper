using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

public sealed record SourceTitle
{
    public const int MaxLength = 120;
    public const string InvalidCode = "source_title_invalid";

    private SourceTitle(string value) => Value = value;

    public string Value { get; }

    public static Result<SourceTitle> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || UnicodeScalars.Count(trimmed) > MaxLength)
        {
            return Error.Validation(InvalidCode, "Source title is invalid.");
        }

        return new SourceTitle(trimmed);
    }
}
