using Api.Modules.Sources.Domain.Chunking;
using Api.SharedKernel;

namespace Api.Modules.Sources.Domain;

/// <summary>An uploaded file whose name, type, size and text passed the file rules, ready to become a version.</summary>
public sealed record SourceUpload(string FileName, SourceFormat Format, SourceText Text)
{
    /// <summary>
    /// Checks a file in the documented order: name, type, size, encoding, emptiness.
    /// <paramref name="content"/> is null when the file was too large to read.
    /// </summary>
    public static Result<SourceUpload> Read(string fileName, long length, byte[]? content)
    {
        if (!SourceFormats.IsStorableFileName(fileName))
        {
            return Error.Validation(SourceFormats.FileNameInvalidCode, "Source file name cannot be stored.");
        }

        var format = SourceFormats.FromFileName(fileName);
        if (!format.IsSuccess)
        {
            return format.Error!;
        }

        if (length > SourceFormats.MaxFileBytes || content is null)
        {
            return Error.Validation(SourceText.TooLargeCode, "Source file is larger than 1 MiB.");
        }

        var text = SourceText.Decode(content);
        if (!text.IsSuccess)
        {
            return text.Error!;
        }

        if (format.Value == SourceFormat.Markdown && !MarkdownChunker.HasBody(text.Value.Value))
        {
            return Error.Validation(SourceText.EmptyCode, "Markdown file has no content besides its frontmatter.");
        }

        return new SourceUpload(fileName, format.Value, text.Value);
    }
}
