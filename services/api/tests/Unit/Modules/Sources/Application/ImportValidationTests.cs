using System.Text;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.FindContentMatches;
using Api.Modules.Sources.Application.ImportSources;
using Api.Modules.Sources.Domain;
using FluentAssertions;

namespace Unit.Modules.Sources.Application;

public sealed class ImportValidationTests
{
    private static readonly UploadedFile File = new("a.md", 3, Encoding.UTF8.GetBytes("# A"));
    private static readonly ImportMetadataEntry Entry = new("A", null, null);

    [Fact]
    public void AnEmptyBatchIsReportedFirst()
    {
        Codes(new ImportSourcesCommand("c", [], null)).Should().Equal("source_batch_empty");
    }

    [Fact]
    public void MoreThanFiftyFilesIsTooLarge()
    {
        var files = Enumerable.Repeat(File, 51).ToList();

        Codes(new ImportSourcesCommand("c", files, Enumerable.Repeat(Entry, 51).ToList())).Should().Equal("source_batch_too_large");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void MetadataMustBePresentWithOneEntryPerFile(int entries)
    {
        IReadOnlyList<ImportMetadataEntry>? metadata = entries < 0 ? null : Enumerable.Repeat(Entry, entries).ToList();

        Codes(new ImportSourcesCommand("c", [File], metadata)).Should().Equal("source_metadata_invalid");
    }

    [Fact]
    public void AValidShapeHasNoErrors()
    {
        Codes(new ImportSourcesCommand("c", [File], [Entry])).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void HashListsHoldOneToFiftyHashes(int count)
    {
        var hashes = Enumerable.Repeat(new string('a', 64), count).ToList();

        new FindContentMatchesValidator().Validate(new FindContentMatchesQuery("c", hashes)).Errors
            .Select(error => error.ErrorCode).Should().Equal("source_hashes_invalid");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08")]
    public void HashesAreSixtyFourLowercaseHexCharacters(string hash)
    {
        new FindContentMatchesValidator().Validate(new FindContentMatchesQuery("c", [hash])).IsValid.Should().BeFalse();
        new FindContentMatchesValidator().Validate(new FindContentMatchesQuery("c", [hash.ToLowerInvariant()])).IsValid
            .Should().Be(hash.Length == 64);
    }

    [Fact]
    public void AMissingHashListIsInvalid()
    {
        new FindContentMatchesValidator().Validate(new FindContentMatchesQuery("c", null)).Errors
            .Select(error => error.ErrorCode).Should().Equal("source_hashes_invalid");
    }

    [Theory]
    [InlineData("notas.md", SourceFormat.Markdown)]
    [InlineData("NOTAS.MARKDOWN", SourceFormat.Markdown)]
    [InlineData("diario.TxT", SourceFormat.Text)]
    public void SupportedExtensionsAreCaseInsensitive(string fileName, SourceFormat expected)
    {
        SourceFormats.FromFileName(fileName).Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("mapa.pdf")]
    [InlineData("sin-extension")]
    [InlineData("notas.md.zip")]
    [InlineData(null)]
    public void OtherFileNamesAreUnsupported(string? fileName)
    {
        SourceFormats.FromFileName(fileName).Error!.Code.Should().Be("source_file_type_unsupported");
    }

    [Fact]
    public void DecodingRejectsInvalidUtf8AndKeepsTheTextRules()
    {
        SourceText.Decode([0xC3, 0x28]).Error!.Code.Should().Be("source_encoding_invalid");
        SourceText.Decode([]).Error!.Code.Should().Be("source_empty");
        SourceText.Decode([0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i']).Value.Value.Should().Be("hi");
    }

    private static string[] Codes(ImportSourcesCommand command) =>
        new ImportSourcesValidator().Validate(command).Errors.Select(error => error.ErrorCode).ToArray();
}
