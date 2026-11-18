using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using FluentAssertions;

namespace Unit.Modules.Sources.Domain;

public sealed class SourceMetadataTests
{
    [Fact]
    public void ValidMetadataIsParsedWithDefaults()
    {
        var metadata = SourceMetadata.Parse(" Sesión 3 ", null, null).Value;

        metadata.Title.Value.Should().Be("Sesión 3");
        metadata.Category.Should().BeNull();
        metadata.KnowledgeStatus.Should().Be(KnowledgeStatus.Unknown);
    }

    [Fact]
    public void EveryInvalidFieldIsReportedInFieldOrder()
    {
        var error = SourceMetadata.Parse(" ", "rumours", "canon").Error!;

        error.Should().Be(Error.InvalidFields("Source metadata is invalid.",
        [
            new FieldError("title", "source_title_invalid"),
            new FieldError("category", "source_category_invalid"),
            new FieldError("knowledgeStatus", "source_knowledge_status_invalid")
        ]));
    }
}
