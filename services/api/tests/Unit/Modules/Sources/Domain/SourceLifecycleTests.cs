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
        var version = SourceVersion.Create(source, SourceText.Create("texto").Value!, null, Now);
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
        var version = SourceVersion.Create(source, SourceText.Create("texto").Value!, null, Now);
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
        var other = SourceVersion.Create(NewSource(campaignId), SourceText.Create("otro").Value!, null, Now);

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

    private static Source NewSource(Guid campaignId) =>
        Source.Create(campaignId, SourceTitle.Create("Sesión 3").Value!, SourceOrigin.Paste, null, KnowledgeStatus.Unknown, Now);
}
