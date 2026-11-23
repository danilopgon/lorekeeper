using Api.Modules.Sources.Domain;
using FluentAssertions;

namespace Unit.Modules.Sources.Domain;

public sealed class SourceLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void ANewSourceIsUnpublishedAndItsVersionAndJobShareItsCampaign()
    {
        var source = NewSource(Guid.NewGuid());
        var version = SourceVersion.Create(source, SourceText.Create("texto").Value, null, Now);
        var job = SourceIngestionJob.Create(version, Now);

        source.PublishedVersionId.Should().BeNull();
        source.CreatedAt.Should().Be(Now.ToUniversalTime()).And.Be(source.UpdatedAt);
        version.CampaignId.Should().Be(source.CampaignId);
        version.SourceId.Should().Be(source.Id);
        job.Should().BeEquivalentTo(new { version.CampaignId, SourceVersionId = version.Id, State = IngestionJobState.Queued, Attempts = 0 });
    }

    [Fact]
    public void PublishingPointsTheSourceAtTheVersion()
    {
        var source = NewSource(Guid.NewGuid());
        var version = SourceVersion.Create(source, SourceText.Create("texto").Value, null, Now);
        var later = Now.AddMinutes(1);

        source.Publish(version, later);

        source.PublishedVersionId.Should().Be(version.Id);
        source.LastPublishedAt.Should().Be(later);
        source.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void ASourceCannotPublishAnotherSourcesVersion()
    {
        var campaignId = Guid.NewGuid();
        var other = SourceVersion.Create(NewSource(campaignId), SourceText.Create("otro").Value, null, Now);

        var act = () => NewSource(campaignId).Publish(other, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    public void TransientFailuresRetryWithGrowingDelayUntilTheLastAttempt(int attempts, int expectedDelaySeconds)
    {
        var outcome = IngestionRetryPolicy.Default.AfterTransientFailure(attempts, Now);

        outcome.Should().Be(IngestionFailureOutcome.Retry(Now.AddSeconds(expectedDelaySeconds)));
    }

    [Fact]
    public void TheThirdTransientFailureFailsAsUnavailable()
    {
        IngestionRetryPolicy.Default.AfterTransientFailure(3, Now)
            .Should().Be(IngestionFailureOutcome.Fail("source_processing_unavailable"));
    }

    [Fact]
    public void UnexpectedFailuresFailImmediately()
    {
        IngestionRetryPolicy.AfterUnexpectedFailure().Should().Be(IngestionFailureOutcome.Fail("source_processing_failed"));
    }

    [Fact]
    public void EditingWithTheCurrentMetadataChangesNothing()
    {
        var source = NewSource(Guid.NewGuid());

        var changed = source.Edit(SourceMetadata.Parse("Sesión 3", null, null).Value, Now.AddMinutes(1));

        changed.Should().BeFalse();
        source.UpdatedAt.Should().Be(Now.ToUniversalTime());
    }

    [Fact]
    public void EditingReplacesTheMetadataAndTouchesTheSource()
    {
        var source = NewSource(Guid.NewGuid());
        var later = Now.AddMinutes(1);

        var changed = source.Edit(SourceMetadata.Parse("Sesión 4", "session_notes", "occurred").Value, later);

        changed.Should().BeTrue();
        source.Should().BeEquivalentTo(new { Category = SourceCategory.SessionNotes, KnowledgeStatus = KnowledgeStatus.Occurred, UpdatedAt = later });
        source.Title.Value.Should().Be("Sesión 4");
    }

    [Fact]
    public void RemovingLeavesATombstoneThatPointsAtNoVersionAndCannotChange()
    {
        var source = NewSource(Guid.NewGuid());
        source.Publish(SourceVersion.Create(source, SourceText.Create("texto").Value, null, Now), Now);
        var later = Now.AddMinutes(1);

        source.Remove(later);

        source.Should().BeEquivalentTo(new { RemovedAt = later, PublishedVersionId = (Guid?)null, IsRemoved = true });
        source.Title.Value.Should().Be("Sesión 3");
        ((Action)(() => source.Remove(later))).Should().Throw<InvalidOperationException>();
        ((Action)(() => source.ReceiveContentUpdate(later))).Should().Throw<InvalidOperationException>();
        ((Action)(() => source.Edit(SourceMetadata.Parse("Otra", null, null).Value, later))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void OnlyAFailedJobCanBeRequeued()
    {
        var source = NewSource(Guid.NewGuid());
        var job = SourceIngestionJob.Create(SourceVersion.Create(source, SourceText.Create("texto").Value, null, Now), Now);

        ((Action)(() => job.Requeue(Now))).Should().Throw<InvalidOperationException>();
        job.IsActive.Should().BeTrue();
    }

    private static Source NewSource(Guid campaignId) =>
        Source.Create(campaignId, SourceMetadata.Parse("Sesión 3", null, null).Value, SourceOrigin.Paste, Now);
}
