using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

public sealed record SourceTitle
{
    public const int MaxLength = 120;
    public const string InvalidCode = "source_title_invalid";

    private SourceTitle(string value) => Value = value;

    public string Value { get; }

    public static DomainResult<SourceTitle> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || UnicodeScalars.Count(trimmed) > MaxLength)
        {
            return DomainResult<SourceTitle>.Failure(InvalidCode);
        }

        return DomainResult<SourceTitle>.Success(new SourceTitle(trimmed));
    }
}
