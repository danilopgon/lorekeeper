using Api.Modules.Sources.Domain;
using Api.Modules.Sources.Domain.Chunking;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.ProcessSourceVersion;

public sealed class ProcessSourceVersionHandler(ISourceRepository sources, TimeProvider timeProvider)
    : IRequestHandler<ProcessSourceVersionCommand, Result<ProcessSourceVersionOutcome>>
{
    public async Task<Result<ProcessSourceVersionOutcome>> Handle(ProcessSourceVersionCommand command, CancellationToken cancellationToken)
    {
        var claim = command.Job;
        var work = await sources.GetProcessingWorkAsync(claim.CampaignId, claim.JobId, cancellationToken);
        if (work is null || work.Source.IsRemoved || !IsCurrentClaim(work.Job, claim))
        {
            return ProcessSourceVersionOutcome.Discarded;
        }

        var text = work.Version.OriginalText;
        var drafts = SourceFormats.ForVersion(work.Version) == SourceFormat.Markdown
            ? MarkdownChunker.Chunk(text)
            : PlainTextChunker.Chunk(text);
        var chunks = drafts.Select(draft => SourceChunk.Create(work.Version, draft));
        sources.ReplaceChunks(work, chunks);

        var supersededVersionId = work.Source.PublishedVersionId;
        var now = timeProvider.GetUtcNow();
        work.Source.Publish(work.Version, now);
        work.Job.Succeed(now);
        if (supersededVersionId is { } superseded && superseded != work.Version.Id)
        {
            sources.DeleteVersion(work.Source.CampaignId, superseded);
        }

        return await sources.SaveChangesAsync(cancellationToken) == SourceSaveOutcome.Saved
            ? ProcessSourceVersionOutcome.Published
            : ProcessSourceVersionOutcome.Discarded;
    }

    /// <summary>
    /// A worker whose lease expired and was reclaimed must not touch the job's later attempt or its publication.
    /// The lease is compared too because a manual retry restarts the attempt count.
    /// </summary>
    private static bool IsCurrentClaim(SourceIngestionJob job, ClaimedIngestionJob claim) =>
        job.State == IngestionJobState.Processing && job.Attempts == claim.Attempts && job.LeaseExpiresAt == claim.LeaseExpiresAt;
}

public sealed record ProcessSourceVersionCommand(ClaimedIngestionJob Job) : IRequest<Result<ProcessSourceVersionOutcome>>;

public sealed record ClaimedIngestionJob(Guid JobId, Guid CampaignId, Guid SourceVersionId, int Attempts, DateTimeOffset LeaseExpiresAt);

public enum ProcessSourceVersionOutcome
{
    Published,
    Discarded
}
