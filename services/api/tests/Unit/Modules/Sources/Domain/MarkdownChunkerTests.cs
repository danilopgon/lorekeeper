using Api.Modules.Sources.Domain.Chunking;
using FluentAssertions;

namespace Unit.Modules.Sources.Domain;

public sealed class MarkdownChunkerTests
{
    [Fact]
    public void EveryHeadingStartsAChunkWithItsHeadingPath()
    {
        const string text = "Intro\n\n# Sesión 3\n\nLlegan.\n\n## Taberna\n\nMirela.\n\n### Sótano\n\nRatas.\n\n## Puente\n\nNiebla.";

        var chunks = MarkdownChunker.Chunk(text);

        chunks.Select(chunk => (string.Join(" > ", chunk.HeadingPath), chunk.StartLine, chunk.EndLine)).Should().Equal(
            ("", 1, 1),
            ("Sesión 3", 3, 5),
            ("Sesión 3 > Taberna", 7, 9),
            ("Sesión 3 > Taberna > Sótano", 11, 13),
            ("Sesión 3 > Puente", 15, 17));
        chunks.Select(chunk => chunk.Ordinal).Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public void ChunkTextIsTheExactSliceIncludingTheHeadingLine()
    {
        const string text = "# Título\r\n\r\nTexto  con  espacios\r\n";

        MarkdownChunker.Chunk(text).Single().Text.Should().Be("# Título\r\n\r\nTexto  con  espacios");
    }

    [Theory]
    [InlineData("#hashtag")]
    [InlineData("    # indentado cuatro espacios")]
    [InlineData("####### siete")]
    [InlineData("Título\n======")]
    public void NonAtxHeadingsDoNotStartAChunk(string line)
    {
        MarkdownChunker.Chunk($"Antes\n\n{line}\n\nDespués").Should().ContainSingle()
            .Which.HeadingPath.Should().BeEmpty();
    }

    [Fact]
    public void ClosingHashesAreNotPartOfTheHeadingText()
    {
        MarkdownChunker.Chunk("  ## Taberna del cruce ##  \n\ntexto").Single().HeadingPath.Should().Equal("Taberna del cruce");
    }

    [Fact]
    public void HeadingsInsideFencedCodeAreIgnoredAndFencesAreNeverSplit()
    {
        var code = string.Join("\n", Enumerable.Repeat("x = 1", 30));
        var text = $"# Código\n\nAntes.\n\n```python\n# no es un título\n\n{code}\n```\n\nDespués.";

        var chunks = MarkdownChunker.Chunk(text, maxLength: 60);

        chunks.Should().OnlyContain(chunk => chunk.HeadingPath.SequenceEqual(new[] { "Código" }));
        chunks.Should().ContainSingle(chunk => chunk.Text.StartsWith("```python", StringComparison.Ordinal))
            .Which.Text.Should().EndWith("```");
    }

    [Fact]
    public void AnUnclosedFenceRunsToTheEndOfTheSection()
    {
        var chunks = MarkdownChunker.Chunk("# A\n\n~~~\n# dentro\n\nsigue", maxLength: 5);

        chunks.Select(chunk => chunk.Text).Should().Equal("# A", "~~~\n# dentro\n\nsigue");
    }

    [Fact]
    public void ATildeFenceIsNotClosedByBackticks()
    {
        var chunks = MarkdownChunker.Chunk("~~~~\n```\n# dentro\n~~~~\n\n# Fuera\n\ntexto");

        chunks.Select(chunk => string.Join(" > ", chunk.HeadingPath)).Should().Equal("", "Fuera");
    }

    [Fact]
    public void TablesStayWholeAndLongSectionsSplitAtParagraphs()
    {
        const string table = "| a | b |\n|---|---|\n| 1 | 2 |\n| 3 | 4 |";
        var text = $"# Datos\n\nUn párrafo largo de introducción.\n\n{table}\n\nCierre.";

        var chunks = MarkdownChunker.Chunk(text, maxLength: 45);

        chunks.Should().Contain(chunk => chunk.Text == table);
        chunks.Should().OnlyContain(chunk => chunk.HeadingPath.SequenceEqual(new[] { "Datos" }));
    }

    [Fact]
    public void ChunksNeverJoinAcrossHeadings()
    {
        MarkdownChunker.Chunk("# A\n\nuno\n\n# B\n\ndos").Should().HaveCount(2);
    }

    [Fact]
    public void FrontmatterIsSkippedButLineNumbersStayRealAndItIsExtracted()
    {
        const string text = "---\ntitle: Sesión 3\ntags: [a]\n---\n# Sesión 3\n\nTexto.";

        var chunk = MarkdownChunker.Chunk(text).Single();

        chunk.StartLine.Should().Be(5);
        chunk.Text.Should().Be("# Sesión 3\n\nTexto.");
        MarkdownSyntax.Frontmatter(text).Should().Be("title: Sesión 3\ntags: [a]");
    }

    [Theory]
    [InlineData("---\nsin cierre\n\nTexto")]
    [InlineData("Texto\n---\nmás")]
    public void ADashLineIsOnlyFrontmatterWhenItOpensTheFileAndIsClosed(string text)
    {
        MarkdownSyntax.Frontmatter(text).Should().BeNull();
        MarkdownChunker.Chunk(text).Should().NotBeEmpty().And.Subject.First().StartLine.Should().Be(1);
    }

    [Theory]
    [InlineData("---\ntitle: x\n---\n\n", false)]
    [InlineData("---\ntitle: x\n...\nTexto", true)]
    [InlineData("# Solo título", true)]
    public void HasBodyIgnoresFrontmatterAndBlankLines(string text, bool expected)
    {
        MarkdownChunker.HasBody(text).Should().Be(expected);
    }
}
