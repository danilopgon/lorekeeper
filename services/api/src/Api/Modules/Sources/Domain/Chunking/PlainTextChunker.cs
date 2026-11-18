using Api.SharedKernel;

namespace Api.Modules.Sources.Domain.Chunking;

public static class PlainTextChunker
{
    public const int DefaultMaxLength = 2_000;

    public static IReadOnlyList<ChunkDraft> Chunk(string text, int maxLength = DefaultMaxLength)
    {
        var lines = new SourceLines(text);
        var chunks = new List<ChunkDraft>();
        (int Start, int End)? current = null;

        foreach (var paragraph in Paragraphs(lines))
        {
            if (current is { } open && UnicodeScalars.Count(lines.Slice(open.Start, paragraph.End)) > maxLength)
            {
                chunks.Add(Draft(lines, chunks.Count, open));
                current = null;
            }

            current = current is { } extended ? (extended.Start, paragraph.End) : paragraph;
        }

        if (current is { } last)
        {
            chunks.Add(Draft(lines, chunks.Count, last));
        }

        return chunks;
    }

    private static IEnumerable<(int Start, int End)> Paragraphs(SourceLines lines)
    {
        int? start = null;

        for (var line = 1; line <= lines.Count; line++)
        {
            if (lines.IsBlank(line))
            {
                if (start is { } open)
                {
                    yield return (open, line - 1);
                    start = null;
                }

                continue;
            }

            start ??= line;
        }

        if (start is { } trailing)
        {
            yield return (trailing, lines.Count);
        }
    }

    private static ChunkDraft Draft(SourceLines lines, int ordinal, (int Start, int End) range) =>
        new(ordinal, [], range.Start, range.End, lines.Slice(range.Start, range.End));
}
