namespace Api.Modules.Sources.Domain.Chunking;

/// <summary>Lines of a text with their offsets, so chunks can be cut as exact slices of the original.</summary>
public sealed class SourceLines
{
    private readonly string _text;
    private readonly List<(int Start, int End)> _lines;

    public SourceLines(string text)
    {
        _text = text;
        _lines = [];

        var start = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n')
            {
                continue;
            }

            var end = index > start && text[index - 1] == '\r' ? index - 1 : index;
            _lines.Add((start, end));
            start = index + 1;
        }

        if (start < text.Length)
        {
            _lines.Add((start, text.Length));
        }
    }

    public int Count => _lines.Count;

    /// <summary>Content of a 1-based line, without its terminator.</summary>
    public string this[int lineNumber]
    {
        get
        {
            var (start, end) = _lines[lineNumber - 1];
            return _text[start..end];
        }
    }

    public bool IsBlank(int lineNumber) => string.IsNullOrWhiteSpace(this[lineNumber]);

    /// <summary>Exact slice of the original text from the start of one line to the end of another, both 1-based and inclusive.</summary>
    public string Slice(int startLine, int endLine) => _text[_lines[startLine - 1].Start.._lines[endLine - 1].End];
}
