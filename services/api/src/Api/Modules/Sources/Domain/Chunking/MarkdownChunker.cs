namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>
/// Cuts Markdown into chunks: every ATX heading starts a section, a long section is split between paragraphs,
/// and fenced code blocks and tables (paragraphs without blank lines) are never split. Frontmatter is skipped.
/// </summary>
public static class MarkdownChunker
{
    public static IReadOnlyList<ChunkDraft> Chunk(string text, int maxLength = PlainTextChunker.DefaultMaxLength)
    {
        var lines = new SourceLines(text);
        var drafts = new List<ChunkDraft>();

        foreach (var section in Sections(lines))
        {
            foreach (var range in ChunkMerger.Merge(lines, Blocks(lines, section.Range), maxLength))
            {
                drafts.Add(new ChunkDraft(drafts.Count, section.HeadingPath, range.Start, range.End, lines.Slice(range)));
            }
        }

        return drafts;
    }

    /// <summary>Whether the Markdown has anything to chunk once frontmatter and blank lines are left out.</summary>
    public static bool HasBody(string text)
    {
        var lines = new SourceLines(text);
        var bodyStart = (MarkdownSyntax.FrontmatterRange(lines)?.End ?? 0) + 1;

        return Enumerable.Range(bodyStart, Math.Max(0, lines.Count - bodyStart + 1)).Any(line => !lines.IsBlank(line));
    }

    private sealed record Section(LineRange Range, IReadOnlyList<string> HeadingPath);

    private static IEnumerable<Section> Sections(SourceLines lines)
    {
        var headings = new List<MarkdownSyntax.Heading>();
        var sectionStart = (MarkdownSyntax.FrontmatterRange(lines)?.End ?? 0) + 1;
        IReadOnlyList<string> path = [];
        string? openFence = null;

        for (var line = sectionStart; line <= lines.Count; line++)
        {
            var text = lines.Text(line);

            if (openFence is not null)
            {
                openFence = MarkdownSyntax.ClosesFence(text, openFence) ? null : openFence;
                continue;
            }

            openFence = MarkdownSyntax.AsFenceOpening(text);
            if (openFence is not null || MarkdownSyntax.AsHeading(text) is not { } heading)
            {
                continue;
            }

            if (line > sectionStart)
            {
                yield return new Section(new LineRange(sectionStart, line - 1), path);
            }

            headings.RemoveAll(open => open.Level >= heading.Level);
            headings.Add(heading);
            path = headings.Select(open => open.Text).ToList();
            sectionStart = line;
        }

        if (sectionStart <= lines.Count)
        {
            yield return new Section(new LineRange(sectionStart, lines.Count), path);
        }
    }

    /// <summary>The unsplittable pieces of a section: its heading line, whole fenced code blocks and paragraphs.</summary>
    private static IEnumerable<LineRange> Blocks(SourceLines lines, LineRange section)
    {
        var line = section.Start;

        while (line <= section.End)
        {
            var text = lines.Text(line);

            if (lines.IsBlank(line))
            {
                line++;
            }
            else if (MarkdownSyntax.AsFenceOpening(text) is { } fence)
            {
                var start = line++;
                while (line <= section.End && !MarkdownSyntax.ClosesFence(lines.Text(line), fence))
                {
                    line++;
                }

                var end = Math.Min(line, section.End);
                yield return new LineRange(start, end);
                line = end + 1;
            }
            else if (MarkdownSyntax.AsHeading(text) is not null)
            {
                yield return new LineRange(line, line);
                line++;
            }
            else
            {
                var start = line;
                while (line <= section.End && !lines.IsBlank(line) && MarkdownSyntax.AsFenceOpening(lines.Text(line)) is null)
                {
                    line++;
                }

                yield return new LineRange(start, line - 1);
            }
        }
    }
}
