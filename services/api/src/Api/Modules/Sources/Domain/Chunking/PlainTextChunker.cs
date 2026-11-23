namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>Cuts plain text into chunks of whole paragraphs, each at most <c>maxLength</c> scalars unless one paragraph alone is longer.</summary>
public static class PlainTextChunker
{
    public const int DefaultMaxLength = 2_000;

    public static IReadOnlyList<ChunkDraft> Chunk(string text, int maxLength = DefaultMaxLength)
    {
        var lines = new SourceLines(text);
        var everything = new LineRange(1, lines.Count);

        return ChunkMerger.Merge(lines, Paragraphs(lines, everything), maxLength)
            .Select((range, ordinal) => new ChunkDraft(ordinal, [], range.Start, range.End, lines.Slice(range)))
            .ToList();
    }

    /// <summary>Runs of non-blank lines inside <paramref name="range"/>.</summary>
    public static IEnumerable<LineRange> Paragraphs(SourceLines lines, LineRange range)
    {
        var line = range.Start;

        while (line <= range.End)
        {
            if (lines.IsBlank(line))
            {
                line++;
                continue;
            }

            var start = line;
            while (line <= range.End && !lines.IsBlank(line))
            {
                line++;
            }

            yield return new LineRange(start, line - 1);
        }
    }
}
