using Api.Modules.Sources.Domain.Chunking;
using FluentAssertions;

namespace Unit.Modules.Sources.Domain;

public sealed class PlainTextChunkerTests
{
    [Fact]
    public void ShortTextIsOneChunkWithRealLineLocators()
    {
        var chunks = PlainTextChunker.Chunk("\n\nPrimera línea\nSegunda línea\n\nOtro párrafo\n\n");

        chunks.Should().ContainSingle();
        chunks[0].Should().BeEquivalentTo(new { Ordinal = 0, StartLine = 3, EndLine = 6, Text = "Primera línea\nSegunda línea\n\nOtro párrafo" });
        chunks[0].HeadingPath.Should().BeEmpty();
    }

    [Fact]
    public void ChunksAreExactSlicesOfTheOriginalTextIncludingCrlf()
    {
        const string text = "Uno\r\ndos\r\n\r\ntres";

        var chunk = PlainTextChunker.Chunk(text).Single();

        chunk.Text.Should().Be(text);
        chunk.EndLine.Should().Be(4);
    }

    [Fact]
    public void LongTextIsSplitAtParagraphBoundaries()
    {
        var paragraph = new string('a', 30);
        var text = string.Join("\n\n", Enumerable.Repeat(paragraph, 5));

        var chunks = PlainTextChunker.Chunk(text, maxLength: 70);

        chunks.Select(chunk => (chunk.StartLine, chunk.EndLine)).Should().Equal((1, 3), (5, 7), (9, 9));
        chunks.Select(chunk => chunk.Ordinal).Should().Equal(0, 1, 2);
        chunks.Should().AllSatisfy(chunk => chunk.Text.Should().NotStartWith("\n").And.NotEndWith("\n"));
    }

    [Fact]
    public void AParagraphLongerThanTheLimitStaysWhole()
    {
        var longParagraph = string.Join("\n", Enumerable.Repeat(new string('b', 50), 3));

        var chunks = PlainTextChunker.Chunk($"corto\n\n{longParagraph}\n\nfinal", maxLength: 60);

        chunks.Select(chunk => (chunk.StartLine, chunk.EndLine)).Should().Equal((1, 1), (3, 5), (7, 7));
        chunks[1].Text.Should().Be(longParagraph);
    }

    [Fact]
    public void TheLimitCountsUnicodeScalarsNotUtf16Units()
    {
        var astral = string.Concat(Enumerable.Repeat("𝔊", 10));

        PlainTextChunker.Chunk($"{astral}\n\n{astral}", maxLength: 22).Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    [InlineData("  \r\n\t\r\n")]
    public void TextWithoutContentHasNoChunks(string text)
    {
        PlainTextChunker.Chunk(text).Should().BeEmpty();
    }

    [Fact]
    public void ASingleLineWithoutALineBreakIsOneChunk()
    {
        var chunk = PlainTextChunker.Chunk("solo una línea").Single();

        chunk.Should().BeEquivalentTo(new { StartLine = 1, EndLine = 1, Text = "solo una línea" });
    }

    [Fact]
    public void LinesWithOnlyWhitespaceSeparateParagraphs()
    {
        var chunks = PlainTextChunker.Chunk("uno\n   \t\ndos", maxLength: 3);

        chunks.Select(chunk => chunk.Text).Should().Equal("uno", "dos");
    }

    [Fact]
    public void ALoneCarriageReturnIsContentNotALineBreak()
    {
        var chunk = PlainTextChunker.Chunk("a\rb\nc").Single();

        chunk.EndLine.Should().Be(2);
        chunk.Text.Should().Be("a\rb\nc");
    }

    [Fact]
    public void ParagraphsThatExactlyFitTheLimitStayTogether()
    {
        PlainTextChunker.Chunk("abc\n\ndef", maxLength: 8).Should().ContainSingle();
        PlainTextChunker.Chunk("abc\n\ndef", maxLength: 7).Should().HaveCount(2);
    }
}
