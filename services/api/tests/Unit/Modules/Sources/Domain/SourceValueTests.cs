using Api.Modules.Sources.Domain;
using FluentAssertions;

namespace Unit.Modules.Sources.Domain;

public sealed class SourceValueTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TitleIsRequired(string? value)
    {
        SourceTitle.Create(value).ErrorCode.Should().Be("source_title_invalid");
    }

    [Fact]
    public void TitleIsTrimmedAndCountsUnicodeScalars()
    {
        var astral = string.Concat(Enumerable.Repeat("𝔊", 120));

        SourceTitle.Create("  Sesión 3  ").Value!.Value.Should().Be("Sesión 3");
        SourceTitle.Create(astral).IsSuccess.Should().BeTrue();
        SourceTitle.Create(astral + "a").ErrorCode.Should().Be("source_title_invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    [InlineData("﻿  ")]
    public void TextMustNotBeEmptyOrWhitespace(string? value)
    {
        SourceText.Create(value).ErrorCode.Should().Be("source_empty");
    }

    [Fact]
    public void TextRemovesTheByteOrderMarkAndKeepsEverythingElse()
    {
        var text = SourceText.Create("﻿# Título\r\n\r\nTexto  ").Value!;

        text.Value.Should().Be("# Título\r\n\r\nTexto  ");
    }

    [Fact]
    public void TextIsLimitedToOneMebibyteOfUtf8()
    {
        var atLimit = new string('a', SourceText.MaxBytes - 2) + "ñ";

        SourceText.Create(atLimit).IsSuccess.Should().BeTrue();
        SourceText.Create(atLimit + "a").ErrorCode.Should().Be("source_too_large");
    }

    [Fact]
    public void TextThatCannotBeStoredAsUtf8TextIsRejected()
    {
        SourceText.Create("texto \uD800").ErrorCode.Should().Be("source_encoding_invalid");
        SourceText.Create("antes\0después").ErrorCode.Should().Be("source_encoding_invalid");
    }

    [Fact]
    public void ContentHashIsLowercaseSha256OfTheUtf8TextWithoutNormalisingLineEndings()
    {
        SourceText.Create("abc").Value!.ContentHash.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        SourceText.Create("﻿abc").Value!.ContentHash.Should().Be(SourceText.Create("abc").Value!.ContentHash);
        SourceText.Create("a\r\nb").Value!.ContentHash.Should().NotBe(SourceText.Create("a\nb").Value!.ContentHash);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("session_notes", SourceCategory.SessionNotes)]
    [InlineData("characters_places", SourceCategory.CharactersPlaces)]
    [InlineData("general_reference", SourceCategory.GeneralReference)]
    public void CategoryIsOptionalAndUsesTheContractNames(string? value, SourceCategory? expected)
    {
        SourceClassification.ParseCategory(value).Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("SessionNotes")]
    [InlineData("")]
    [InlineData("rumours")]
    public void UnknownCategoriesAreRejected(string value)
    {
        SourceClassification.ParseCategory(value).ErrorCode.Should().Be("source_category_invalid");
    }

    [Fact]
    public void KnowledgeStatusDefaultsToUnknownAndRejectsOtherValues()
    {
        SourceClassification.ParseKnowledgeStatus(null).Value.Should().Be(KnowledgeStatus.Unknown);
        SourceClassification.ParseKnowledgeStatus("occurred").Value.Should().Be(KnowledgeStatus.Occurred);
        SourceClassification.ParseKnowledgeStatus("canon").ErrorCode.Should().Be("source_knowledge_status_invalid");
    }
}
