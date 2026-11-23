using System.Text.RegularExpressions;

namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>The few CommonMark rules the chunker needs: ATX headings, fenced code blocks and YAML frontmatter.</summary>
public static partial class MarkdownSyntax
{
    public sealed record Heading(int Level, string Text);

    /// <summary>An ATX heading: up to three spaces, one to six <c>#</c>, then a space or the end of the line. Setext headings are not recognised.</summary>
    public static Heading? AsHeading(string line)
    {
        var match = AtxHeading().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var text = ClosingHashes().Replace(match.Groups["text"].Value, string.Empty).Trim();
        return new Heading(match.Groups["level"].Length, text);
    }

    /// <summary>
    /// The opening of a fenced code block: up to three spaces, then at least three backticks or tildes.
    /// After backticks the info string may not contain a backtick, so <c>```code```</c> is an inline span, not a fence.
    /// </summary>
    public static string? AsFenceOpening(string line)
    {
        var match = FenceOpening().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var fence = match.Groups["fence"].Value;
        return fence[0] == '`' && match.Groups["info"].Value.Contains('`', StringComparison.Ordinal) ? null : fence;
    }

    /// <summary>Whether <paramref name="line"/> closes a fence opened with <paramref name="opening"/>: the same character, at least as many times, nothing else.</summary>
    public static bool ClosesFence(string line, string opening)
    {
        var match = FenceClosing().Match(line);
        return match.Success
            && match.Groups["fence"].Value[0] == opening[0]
            && match.Groups["fence"].Length >= opening.Length;
    }

    /// <summary>YAML frontmatter: line 1 is <c>---</c> and a later line is <c>---</c> or <c>...</c>. Returns the delimiter lines too.</summary>
    public static LineRange? FrontmatterRange(SourceLines lines)
    {
        if (lines.Count < 2 || lines.Text(1).TrimEnd() != "---")
        {
            return null;
        }

        for (var line = 2; line <= lines.Count; line++)
        {
            if (lines.Text(line).TrimEnd() is "---" or "...")
            {
                return new LineRange(1, line);
            }
        }

        return null;
    }

    /// <summary>The YAML between the frontmatter delimiters, or <c>null</c> when the text has no frontmatter.</summary>
    public static string? Frontmatter(string text)
    {
        var lines = new SourceLines(text);
        if (FrontmatterRange(lines) is not { } range)
        {
            return null;
        }

        return range.End - range.Start < 2 ? string.Empty : lines.Slice(new LineRange(range.Start + 1, range.End - 1));
    }

    [GeneratedRegex(@"\A {0,3}(?<level>#{1,6})(?:[ \t]+(?<text>.*))?\z")]
    private static partial Regex AtxHeading();

    [GeneratedRegex(@"(?:\A|[ \t]+)#+[ \t]*\z")]
    private static partial Regex ClosingHashes();

    [GeneratedRegex(@"\A {0,3}(?<fence>`{3,}|~{3,})(?<info>.*)\z")]
    private static partial Regex FenceOpening();

    [GeneratedRegex(@"\A {0,3}(?<fence>`{3,}|~{3,})[ \t]*\z")]
    private static partial Regex FenceClosing();
}
