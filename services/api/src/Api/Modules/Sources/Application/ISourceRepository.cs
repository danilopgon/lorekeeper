using Api.Modules.Sources.Domain;

namespace Api.Modules.Sources.Application;

public interface ISourceRepository
{
    void Add(Source source, SourceVersion version, SourceIngestionJob job);

    Task<SourceProcessingWork?> GetProcessingWorkAsync(Guid campaignId, Guid jobId, CancellationToken cancellationToken);

    void ReplaceChunks(SourceProcessingWork work, IEnumerable<SourceChunk> chunks);

    Task<SourceSaveResult> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record SourceProcessingWork(Source Source, SourceVersion Version, SourceIngestionJob Job, IReadOnlyList<SourceChunk> ExistingChunks);

public enum SourceSaveResult
{
    Saved,
    Discarded
}
