using Api.SharedKernel;

namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>Greedily joins consecutive blocks while the joined text stays within the limit; a block is never split.</summary>
public static class ChunkMerger
{
    public static IReadOnlyList<LineRange> Merge(SourceLines lines, IEnumerable<LineRange> blocks, int maxLength)
    {
        var chunks = new List<LineRange>();

        foreach (var block in blocks)
        {
            if (chunks.Count > 0 && Fits(lines, chunks[^1] with { End = block.End }, maxLength))
            {
                chunks[^1] = chunks[^1] with { End = block.End };
            }
            else
            {
                chunks.Add(block);
            }
        }

        return chunks;
    }

    private static bool Fits(SourceLines lines, LineRange range, int maxLength) =>
        UnicodeScalars.Count(lines.Slice(range)) <= maxLength;
}
