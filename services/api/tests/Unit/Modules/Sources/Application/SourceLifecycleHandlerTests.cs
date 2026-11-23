using System.Text;
using Api.Modules.Campaigns.Application;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.EditSource;
using Api.Modules.Sources.Application.RemoveSource;
using Api.Modules.Sources.Application.RetrySource;
using Api.Modules.Sources.Application.UpdateSourceContent;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Unit.Modules.Sources.Application;

public sealed class SourceLifecycleHandlerTests
{
    private static readonly Guid CampaignId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");

    private readonly ICampaignQueries _campaigns = Substitute.For<ICampaignQueries>();
    private readonly ISourceRepository _repository = Substitute.For<ISourceRepository>();
    private readonly ISourceQueries _queries = Substitute.For<ISourceQueries>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));
    private readonly Source _source;

    public SourceLifecycleHandlerTests()
    {
        _campaigns.GetByIdAsync(CampaignId, Arg.Any<CancellationToken>())
            .Returns(new CampaignDto(CampaignId.ToString("D"), "Ash Crown", _time.GetUtcNow(), _time.GetUtcNow()));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(SourceSaveOutcome.Saved);
        _source = Source.Create(CampaignId, SourceMetadata.Parse("Sesión 3", "session_notes", "occurred").Value, SourceOrigin.Paste, _time.GetUtcNow());
        _queries.GetAsync(CampaignId, _source.Id, Arg.Any<CancellationToken>()).Returns(Dto());
        _time.Advance(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("not-a-uuid", "11111111-1111-1111-1111-111111111111", "campaign_id_invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111", "11111111-1111-1111-1111-111111111111", "campaign_not_found")]
    [InlineData("7c9e6679-7425-40de-944b-e07fc1f90ae7", "not-a-uuid", "source_id_invalid")]
    [InlineData("7c9e6679-7425-40de-944b-e07fc1f90ae7", "11111111-1111-1111-1111-111111111111", "source_not_found")]
    public async Task EveryLifecycleRouteResolvesCampaignThenSource(string campaignId, string sourceId, string expected)
    {
        var errors = new[]
        {
            (await UpdateHandler().Handle(new UpdateSourceContentCommand(campaignId, sourceId, [], ["Texto"]), CancellationToken.None)).Error!,
            (await EditHandler().Handle(new EditSourceCommand(campaignId, sourceId, null, null, null), CancellationToken.None)).Error!,
            (await RetryHandler().Handle(new RetrySourceCommand(campaignId, sourceId), CancellationToken.None)).Error!,
            (await RemoveHandler().Handle(new RemoveSourceCommand(campaignId, sourceId), CancellationToken.None)).Error!
        };

        errors.Select(error => error.Code).Should().AllBe(expected);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, "Texto")]
    [InlineData(false, null)]
    public async Task AnUpdateNeedsExactlyOneOfFileAndText(bool withFile, string? text)
    {
        Lock();
        var file = withFile ? File("notas.md", "# Notas") : null;

        var result = await UpdateHandler().Handle(Update(file, text), CancellationToken.None);

        result.Error!.Code.Should().Be("source_content_part_invalid");
        await _repository.DidNotReceive().LockAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RepeatedContentPartsAreRejected()
    {
        Lock();
        var sourceId = _source.Id.ToString("D");

        var texts = await UpdateHandler().Handle(new UpdateSourceContentCommand(CampaignId.ToString("D"), sourceId, [], ["A", "B"]), CancellationToken.None);
        var files = await UpdateHandler().Handle(
            new UpdateSourceContentCommand(CampaignId.ToString("D"), sourceId, [File("a.md", "# A"), File("b.md", "# B")], []),
            CancellationToken.None);

        texts.Error!.Code.Should().Be("source_content_part_invalid");
        files.Error!.Code.Should().Be("source_content_part_invalid");
        _repository.DidNotReceiveWithAnyArgs().AddVersion(default!, default!);
    }

    [Theory]
    [InlineData("", "text", "source_empty")]
    [InlineData("a\0b", "text", "source_encoding_invalid")]
    public async Task InvalidTextIsAFieldError(string text, string field, string code)
    {
        Lock();

        var result = await UpdateHandler().Handle(Update(null, text), CancellationToken.None);

        result.Error!.Fields.Should().Equal(new FieldError(field, code));
        _repository.DidNotReceiveWithAnyArgs().AddVersion(default!, default!);
    }

    [Theory]
    [InlineData("mapa.pdf", "source_file_type_unsupported")]
    [InlineData("a\0.md", "source_file_name_invalid")]
    public async Task InvalidFilesAreFieldErrors(string fileName, string code)
    {
        Lock();

        var result = await UpdateHandler().Handle(Update(File(fileName, "texto"), null), CancellationToken.None);

        result.Error!.Fields.Should().Equal(new FieldError("file", code));
    }

    [Theory]
    [InlineData(IngestionJobState.Queued)]
    [InlineData(IngestionJobState.Processing)]
    public async Task AnUpdateIsRefusedWhileAVersionIsQueuedOrProcessing(IngestionJobState state)
    {
        Lock(pending: Pending("Pendiente", state));

        var result = await UpdateHandler().Handle(Update(null, "Otro"), CancellationToken.None);

        result.Error.Should().BeEquivalentTo(new { Code = "source_processing", Kind = ErrorKind.Conflict });
        _repository.DidNotReceiveWithAnyArgs().AddVersion(default!, default!);
    }

    [Fact]
    public async Task ContentEqualToThePublishedVersionChangesNothingAndKeepsAFailedUpdate()
    {
        Lock(pending: Pending("Fallida", IngestionJobState.Failed), publishedText: "Igual");

        var result = await UpdateHandler().Handle(Update(null, "Igual"), CancellationToken.None);

        result.Value.Queued.Should().BeFalse();
        _repository.DidNotReceiveWithAnyArgs().AddVersion(default!, default!);
        _repository.DidNotReceiveWithAnyArgs().DeleteVersion(default, default);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnUpdateQueuesTextAsANewVersionWithoutAFileName()
    {
        var before = _source.UpdatedAt;
        Lock(publishedText: "Viejo");

        var result = await UpdateHandler().Handle(Update(null, "Nuevo"), CancellationToken.None);

        result.Value.Queued.Should().BeTrue();
        _repository.Received(1).AddVersion(
            Arg.Is<SourceVersion>(version => version.SourceId == _source.Id && version.OriginalText == "Nuevo" && version.FileName == null),
            Arg.Is<SourceIngestionJob>(job => job.State == IngestionJobState.Queued));
        _source.UpdatedAt.Should().BeAfter(before);
        _source.Origin.Should().Be(SourceOrigin.Paste);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AFileUpdatesAPastedSourceAndKeepsItsOrigin()
    {
        Lock();

        await UpdateHandler().Handle(Update(File("notas.md", "---\nx: 1\n---\n# Notas"), null), CancellationToken.None);

        _repository.Received(1).AddVersion(
            Arg.Is<SourceVersion>(version => version.FileName == "notas.md" && version.Frontmatter == "x: 1"),
            Arg.Any<SourceIngestionJob>());
        _source.Origin.Should().Be(SourceOrigin.Paste);
    }

    [Fact]
    public async Task AnUpdateDiscardsAFailedPendingVersion()
    {
        var failed = Pending("Fallida", IngestionJobState.Failed);
        Lock(pending: failed, publishedText: "Viejo");

        await UpdateHandler().Handle(Update(null, "Nuevo"), CancellationToken.None);

        _repository.Received(1).DeleteVersion(CampaignId, failed.Version.Id);
    }

    [Fact]
    public async Task WithoutAPublishedVersionTheSameContentAsTheFailedOneIsQueuedAgain()
    {
        var failed = Pending("Igual", IngestionJobState.Failed);
        Lock(pending: failed);

        var result = await UpdateHandler().Handle(Update(null, "Igual"), CancellationToken.None);

        result.Value.Queued.Should().BeTrue();
        _repository.Received(1).DeleteVersion(CampaignId, failed.Version.Id);
        _repository.ReceivedWithAnyArgs(1).AddVersion(default!, default!);
    }

    [Fact]
    public async Task AnEditChangesOnlyTheFieldsItContains()
    {
        Lock();

        await EditHandler().Handle(Edit(title: new FieldEdit(" Sesión 4 "), category: new FieldEdit(null)), CancellationToken.None);

        _source.Title.Value.Should().Be("Sesión 4");
        _source.Category.Should().BeNull();
        _source.KnowledgeStatus.Should().Be(KnowledgeStatus.Occurred);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnEmptyEditOrOneWithTheCurrentValuesSavesNothing()
    {
        Lock();
        var before = _source.UpdatedAt;

        var empty = await EditHandler().Handle(Edit(), CancellationToken.None);
        var same = await EditHandler().Handle(Edit(title: new FieldEdit("Sesión 3"), knowledgeStatus: new FieldEdit("occurred")), CancellationToken.None);

        empty.IsSuccess.Should().BeTrue();
        same.IsSuccess.Should().BeTrue();
        _source.UpdatedAt.Should().Be(before);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnEditReportsEveryInvalidFieldIncludingExplicitNulls()
    {
        Lock();

        var result = await EditHandler().Handle(
            Edit(title: new FieldEdit(null), category: new FieldEdit("rumours"), knowledgeStatus: new FieldEdit(null)),
            CancellationToken.None);

        result.Error!.Code.Should().Be("source_title_invalid");
        result.Error.Fields.Should().Equal(
            new FieldError("title", "source_title_invalid"),
            new FieldError("category", "source_category_invalid"),
            new FieldError("knowledgeStatus", "source_knowledge_status_invalid"));
        await _repository.DidNotReceive().LockAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ARetryRequeuesTheFailedVersionWithFreshAttempts()
    {
        var failed = Pending("Fallida", IngestionJobState.Failed);
        Lock(pending: failed);

        var result = await RetryHandler().Handle(new RetrySourceCommand(CampaignId.ToString("D"), _source.Id.ToString("D")), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        failed.Job.Should().BeEquivalentTo(new { State = IngestionJobState.Queued, Attempts = 0, ErrorCode = (string?)null });
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(IngestionJobState.Queued)]
    [InlineData(IngestionJobState.Processing)]
    public async Task ARetryNeedsAFailedVersion(IngestionJobState? state)
    {
        Lock(pending: state is { } pendingState ? Pending("Pendiente", pendingState) : null);

        var result = await RetryHandler().Handle(new RetrySourceCommand(CampaignId.ToString("D"), _source.Id.ToString("D")), CancellationToken.None);

        result.Error.Should().BeEquivalentTo(new { Code = "source_retry_unavailable", Kind = ErrorKind.Conflict });
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ARemovalCommittedBeforeTheResponseIsReadAnswersNotFound()
    {
        Lock(pending: Pending("Fallida", IngestionJobState.Failed), publishedText: "Viejo");
        _queries.GetAsync(CampaignId, _source.Id, Arg.Any<CancellationToken>()).Returns((SourceDto?)null);
        var sourceId = _source.Id.ToString("D");

        var errors = new[]
        {
            (await UpdateHandler().Handle(Update(null, "Nuevo"), CancellationToken.None)).Error!,
            (await EditHandler().Handle(Edit(title: new FieldEdit("Otra")), CancellationToken.None)).Error!,
            (await RetryHandler().Handle(new RetrySourceCommand(CampaignId.ToString("D"), sourceId), CancellationToken.None)).Error!
        };

        errors.Select(error => error.Code).Should().AllBe("source_not_found");
    }

    [Fact]
    public async Task RemovalLeavesATombstoneAndDeletesEveryVersion()
    {
        var published = SourceVersion.Create(_source, SourceText.Create("texto").Value, null, _time.GetUtcNow());
        _source.Publish(published, _time.GetUtcNow());
        Lock();

        var result = await RemoveHandler().Handle(new RemoveSourceCommand(CampaignId.ToString("D"), _source.Id.ToString("D")), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _source.RemovedAt.Should().Be(_time.GetUtcNow());
        _source.PublishedVersionId.Should().BeNull();
        _repository.Received(1).DeleteAllVersions(_source);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void Lock(PendingVersion? pending = null, string? publishedText = null) =>
        _repository.LockAsync(CampaignId, _source.Id, Arg.Any<CancellationToken>())
            .Returns(new LockedSource(_source, pending, publishedText is null ? null : SourceText.Create(publishedText).Value.ContentHash));

    private PendingVersion Pending(string text, IngestionJobState state)
    {
        var version = SourceVersion.Create(_source, SourceText.Create(text).Value, null, _time.GetUtcNow());
        var job = SourceIngestionJob.Create(version, _time.GetUtcNow());
        typeof(SourceIngestionJob).GetProperty(nameof(SourceIngestionJob.State))!.SetValue(job, state);
        typeof(SourceIngestionJob).GetProperty(nameof(SourceIngestionJob.Attempts))!.SetValue(job, 3);
        return new PendingVersion(version, job);
    }

    private static UploadedFile File(string name, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new UploadedFile(name, bytes.Length, bytes);
    }

    private UpdateSourceContentCommand Update(UploadedFile? file, string? text) =>
        new(CampaignId.ToString("D"), _source.Id.ToString("D"), file is null ? [] : [file], text is null ? [] : [text]);

    private EditSourceCommand Edit(FieldEdit? title = null, FieldEdit? category = null, FieldEdit? knowledgeStatus = null) =>
        new(CampaignId.ToString("D"), _source.Id.ToString("D"), title, category, knowledgeStatus);

    private UpdateSourceContentHandler UpdateHandler() => new(new UpdateSourceContentValidator(), new SourceLookup(_campaigns), _repository, _queries, _time);

    private EditSourceHandler EditHandler() => new(new SourceLookup(_campaigns), _repository, _queries, _time);

    private RetrySourceHandler RetryHandler() => new(new SourceLookup(_campaigns), _repository, _queries, _time);

    private RemoveSourceHandler RemoveHandler() => new(new SourceLookup(_campaigns), _repository, _time);

    private SourceDto Dto() => new(
        _source.Id.ToString("D"),
        CampaignId.ToString("D"),
        "Sesión 3",
        SourceOriginDto.Paste,
        SourceCategoryDto.SessionNotes,
        KnowledgeStatusDto.Occurred,
        SourceAvailabilityDto.Available,
        null,
        null,
        _time.GetUtcNow(),
        _time.GetUtcNow());
}
