using Api.SharedKernel;

namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>Cuts plain text into chunks of whole paragraphs, each at most <c>maxLength</c> scalars unless one paragraph alone is longer.</summary>
public static class PlainTextChunker
{
    public const int DefaultMaxLength = 2_000;

    public static IReadOnlyList<ChunkDraft> Chunk(string text, int maxLength = DefaultMaxLength)
    {
        var lines = new SourceLines(text);
        var chunks = new List<LineRange>();

        foreach (var paragraph in Paragraphs(lines))
        {
            if (chunks.Count > 0 && Fits(lines, chunks[^1] with { End = paragraph.End }, maxLength))
            {
                chunks[^1] = chunks[^1] with { End = paragraph.End };
            }
            else
            {
                chunks.Add(paragraph);
            }
        }

        return chunks
            .Select((range, ordinal) => new ChunkDraft(ordinal, [], range.Start, range.End, lines.Slice(range)))
            .ToList();
    }

    private static bool Fits(SourceLines lines, LineRange range, int maxLength) =>
        UnicodeScalars.Count(lines.Slice(range)) <= maxLength;

    private static IEnumerable<LineRange> Paragraphs(SourceLines lines)
    {
        var line = 1;

        while (line <= lines.Count)
        {
            if (lines.IsBlank(line))
            {
                line++;
                continue;
            }

            var start = line;
            while (line <= lines.Count && !lines.IsBlank(line))
            {
                line++;
            }

            yield return new LineRange(start, line - 1);
        }
    }
}
