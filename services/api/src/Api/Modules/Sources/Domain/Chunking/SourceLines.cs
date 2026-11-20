namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>The lines of a text, numbered from 1, so chunks are exact slices of the original.</summary>
public sealed class SourceLines
{
    private readonly string _text;
    private readonly List<Line> _lines;

    public SourceLines(string text)
    {
        _text = text;
        _lines = Split(text);
    }

    public int Count => _lines.Count;

    public bool IsBlank(int lineNumber) => string.IsNullOrWhiteSpace(Text(lineNumber));

    /// <summary>The content of one line, without its line break.</summary>
    public string Text(int lineNumber) => Content(_lines[lineNumber - 1]);

    /// <summary>The original text from the start of <paramref name="range"/>'s first line to the end of its last, without the final line break.</summary>
    public string Slice(LineRange range) => _text[_lines[range.Start - 1].Start.._lines[range.End - 1].End];

    private string Content(Line line) => _text[line.Start..line.End];

    private static List<Line> Split(string text)
    {
        var lines = new List<Line>();
        var start = 0;

        while (start < text.Length)
        {
            var newline = text.IndexOf('\n', start);
            var next = newline < 0 ? text.Length : newline + 1;
            lines.Add(new Line(start, EndWithoutLineBreak(text, start, next)));
            start = next;
        }

        return lines;
    }

    private static int EndWithoutLineBreak(string text, int start, int next)
    {
        if (next == start || text[next - 1] != '\n')
        {
            return next;
        }

        var end = next - 1;
        return end > start && text[end - 1] == '\r' ? end - 1 : end;
    }

    /// <summary>Offsets of one line's content in the text; <see cref="End"/> excludes the line break.</summary>
    private readonly record struct Line(int Start, int End);
}

/// <summary>A run of lines, 1-based and inclusive at both ends.</summary>
public readonly record struct LineRange(int Start, int End);
