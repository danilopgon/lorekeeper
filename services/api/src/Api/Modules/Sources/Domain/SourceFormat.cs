using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

public enum SourceFormat
{
    Markdown,
    Text
}

public static class SourceFormats
{
    public const string UnsupportedCode = "source_file_type_unsupported";

    /// <summary>Largest accepted upload: 1 MiB of text plus a 3-byte UTF-8 byte order mark.</summary>
    public const int MaxFileBytes = SourceText.MaxBytes + 3;

    public static Result<SourceFormat> FromFileName(string? fileName) =>
        Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
        {
            ".md" or ".markdown" => SourceFormat.Markdown,
            ".txt" => SourceFormat.Text,
            _ => Error.Validation(UnsupportedCode, "Only .md, .markdown and .txt files are supported.")
        };

    /// <summary>Pasted text and .txt files are plain text; only Markdown file names enable heading-aware chunking.</summary>
    public static SourceFormat ForVersion(SourceVersion version)
    {
        var format = FromFileName(version.FileName);
        return format.IsSuccess ? format.Value : SourceFormat.Text;
    }
}
