using Api.Modules.Campaigns.Application;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.ListSources;
using Api.Modules.Sources.Application.PasteSource;
using Api.Modules.Sources.Application.ProcessSourceVersion;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Unit.Modules.Sources.Application;

public sealed class SourceHandlerTests
{
    private static readonly Guid CampaignId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");

    private readonly ICampaignQueries _campaigns = Substitute.For<ICampaignQueries>();
    private readonly ISourceRepository _repository = Substitute.For<ISourceRepository>();
    private readonly ISourceQueries _queries = Substitute.For<ISourceQueries>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

    public SourceHandlerTests()
    {
        _campaigns.GetByIdAsync(CampaignId, Arg.Any<CancellationToken>())
            .Returns(new CampaignDto(CampaignId.ToString("D"), "Ash Crown", _time.GetUtcNow(), _time.GetUtcNow()));
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(SourceSaveOutcome.Saved);
    }

    [Fact]
    public async Task PasteAddsASourceWithItsVersionAndQueuedJobInOneSave()
    {
        var dto = SourceDtoFor(Guid.NewGuid());
        _queries.GetAsync(CampaignId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await PasteHandler().Handle(
            new PasteSourceCommand(CampaignId.ToString("D"), " Ideas ", "Texto", "preparation", "preparation"),
            CancellationToken.None);

        result.Value.Should().Be(dto);
        _repository.Received(1).Add(
            Arg.Is<Source>(source => source.CampaignId == CampaignId && source.Title.Value == "Ideas" && source.Origin == SourceOrigin.Paste
                && source.Category == SourceCategory.Preparation && source.KnowledgeStatus == KnowledgeStatus.Preparation),
            Arg.Is<SourceVersion>(version => version.OriginalText == "Texto" && version.FileName == null),
            Arg.Is<SourceIngestionJob>(job => job.State == IngestionJobState.Queued));
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasteReportsEveryInvalidFieldWithoutSaving()
    {
        var result = await PasteHandler().Handle(
            new PasteSourceCommand(CampaignId.ToString("D"), " ", "", "rumours", "canon"),
            CancellationToken.None);

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Code.Should().Be("source_title_invalid");
        result.Error.Fields.Should().Equal(
            new FieldError("title", "source_title_invalid"),
            new FieldError("category", "source_category_invalid"),
            new FieldError("knowledgeStatus", "source_knowledge_status_invalid"),
            new FieldError("text", "source_empty"));
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-uuid", "campaign_id_invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111", "campaign_not_found")]
    public async Task PasteRequiresAnExistingCampaign(string campaignId, string expected)
    {
        var result = await PasteHandler().Handle(new PasteSourceCommand(campaignId, "Ideas", "Texto", null, null), CancellationToken.None);

        result.Error!.Code.Should().Be(expected);
        _repository.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-uuid", "campaign_id_invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111", "campaign_not_found")]
    public async Task ListRequiresAnExistingCampaign(string campaignId, string expected)
    {
        var result = await new ListSourcesHandler(_campaigns, _queries).Handle(new ListSourcesQuery(campaignId), CancellationToken.None);

        result.Error!.Code.Should().Be(expected);
        await _queries.DidNotReceiveWithAnyArgs().ListAsync(default, default);
    }

    [Fact]
    public async Task ListReturnsTheCampaignSourcesFromTheReadInterface()
    {
        IReadOnlyList<SourceDto> sources = [SourceDtoFor(Guid.NewGuid())];
        _queries.ListAsync(CampaignId, Arg.Any<CancellationToken>()).Returns(sources);

        var result = await new ListSourcesHandler(_campaigns, _queries).Handle(new ListSourcesQuery(CampaignId.ToString("D")), CancellationToken.None);

        result.Value.Should().BeSameAs(sources);
    }

    [Fact]
    public async Task ProcessingChunksPublishesAndSucceedsTheJob()
    {
        var work = WorkFor("Uno\n\nDos");
        _repository.GetProcessingWorkAsync(CampaignId, work.Job.Id, Arg.Any<CancellationToken>()).Returns(work);

        var outcome = await ProcessHandler().ProcessAsync(Claimed(work.Job), CancellationToken.None);

        outcome.Should().Be(ProcessSourceVersionOutcome.Published);
        work.Source.PublishedVersionId.Should().Be(work.Version.Id);
        work.Job.State.Should().Be(IngestionJobState.Succeeded);
        _repository.Received(1).ReplaceChunks(work, Arg.Is<IEnumerable<SourceChunk>>(chunks =>
            chunks.Single().Text == "Uno\n\nDos" && chunks.Single().SourceVersionId == work.Version.Id));
    }

    [Fact]
    public async Task ProcessingDiscardsWorkWhoseJobNoLongerExists()
    {
        var job = WorkFor("texto").Job;

        var outcome = await ProcessHandler().ProcessAsync(Claimed(job), CancellationToken.None);

        outcome.Should().Be(ProcessSourceVersionOutcome.Discarded);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessingIsDiscardedWhenTheSaveLosesToAConcurrentChange()
    {
        var work = WorkFor("texto");
        _repository.GetProcessingWorkAsync(CampaignId, work.Job.Id, Arg.Any<CancellationToken>()).Returns(work);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(SourceSaveOutcome.Discarded);

        var outcome = await ProcessHandler().ProcessAsync(Claimed(work.Job), CancellationToken.None);

        outcome.Should().Be(ProcessSourceVersionOutcome.Discarded);
    }

    private PasteSourceHandler PasteHandler() => new(_campaigns, _repository, _queries, _time);

    private ProcessSourceVersionHandler ProcessHandler() => new(_repository, _time);

    private SourceProcessingWork WorkFor(string text)
    {
        var source = Source.Create(CampaignId, SourceMetadata.Parse("Notas", null, null).Value, SourceOrigin.Paste, _time.GetUtcNow());
        var version = SourceVersion.Create(source, SourceText.Create(text).Value, null, _time.GetUtcNow());
        return new SourceProcessingWork(source, version, SourceIngestionJob.Create(version, _time.GetUtcNow()), []);
    }

    private static ClaimedIngestionJob Claimed(SourceIngestionJob job) => new(job.Id, job.CampaignId, job.SourceVersionId, 1);

    private SourceDto SourceDtoFor(Guid id) => new(
        id.ToString("D"),
        CampaignId.ToString("D"),
        "Ideas",
        SourceOriginDto.Paste,
        null,
        KnowledgeStatusDto.Unknown,
        SourceAvailabilityDto.Unavailable,
        null,
        new SourceProcessingDto(SourceProcessingStateDto.Queued, null, 0),
        _time.GetUtcNow(),
        _time.GetUtcNow());
}
