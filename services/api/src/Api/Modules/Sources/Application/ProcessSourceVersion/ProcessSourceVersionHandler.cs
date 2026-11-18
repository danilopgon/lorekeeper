using Api.Modules.Sources.Domain;
using Api.Modules.Sources.Domain.Chunking;

namespace Api.Modules.Sources.Application.ProcessSourceVersion;

public interface ISourceVersionProcessor
{
    Task<ProcessSourceVersionOutcome> ProcessAsync(ClaimedIngestionJob job, CancellationToken cancellationToken);
}

public sealed class ProcessSourceVersionHandler(ISourceRepository sources, TimeProvider timeProvider) : ISourceVersionProcessor
{
    public async Task<ProcessSourceVersionOutcome> ProcessAsync(ClaimedIngestionJob job, CancellationToken cancellationToken)
    {
        var work = await sources.GetProcessingWorkAsync(job.CampaignId, job.JobId, cancellationToken);
        if (work is null || work.Source.IsRemoved)
        {
            return ProcessSourceVersionOutcome.Discarded;
        }

        var chunks = PlainTextChunker.Chunk(work.Version.OriginalText)
            .Select(draft => SourceChunk.Create(work.Version, draft));
        sources.ReplaceChunks(work, chunks);

        var now = timeProvider.GetUtcNow();
        work.Source.Publish(work.Version, now);
        work.Job.Succeed(now);

        return await sources.SaveChangesAsync(cancellationToken) == SourceSaveResult.Saved
            ? ProcessSourceVersionOutcome.Published
            : ProcessSourceVersionOutcome.Discarded;
    }
}

public sealed record ClaimedIngestionJob(Guid JobId, Guid CampaignId, Guid SourceVersionId, int Attempts);

public enum ProcessSourceVersionOutcome
{
    Published,
    Discarded
}
