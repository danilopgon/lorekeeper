using Api.Modules.Sources.Domain;

namespace Api.Modules.Sources.Application;

/// <summary>
/// Writes of the Sources module. <see cref="LockAsync"/> and <see cref="GetProcessingWorkAsync"/> lock the source row
/// until <see cref="SaveChangesAsync"/> commits; without a save the lock is released and every change rolled back (`03`).
/// </summary>
public interface ISourceRepository
{
    void Add(Source source, SourceVersion version, SourceIngestionJob job);

    /// <summary>Locks a non-removed source and loads its pending version and published content hash; null when it does not exist.</summary>
    Task<LockedSource?> LockAsync(Guid campaignId, Guid sourceId, CancellationToken cancellationToken);

    void AddVersion(SourceVersion version, SourceIngestionJob job);

    /// <summary>Deletes the version with its chunks and job when the changes are saved, after the source stops pointing at it.</summary>
    void DeleteVersion(Guid campaignId, Guid versionId);

    /// <summary>Deletes every version of a removed source with their chunks and jobs when the changes are saved.</summary>
    void DeleteAllVersions(Source source);

    /// <summary>Locks the job's source and loads the work; null when the job, its version or a non-removed source is gone.</summary>
    Task<SourceProcessingWork?> GetProcessingWorkAsync(Guid campaignId, Guid jobId, CancellationToken cancellationToken);

    void ReplaceChunks(SourceProcessingWork work, IEnumerable<SourceChunk> chunks);

    Task<SourceSaveOutcome> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Ends a unit of work that writes nothing: discards pending changes and frees the source lock at once.</summary>
    Task ReleaseAsync(CancellationToken cancellationToken);
}

public sealed record LockedSource(Source Source, PendingVersion? Pending, string? PublishedContentHash);

/// <summary>The one version of a source that is not published, with its job; a source has at most one (`03`).</summary>
public sealed record PendingVersion(SourceVersion Version, SourceIngestionJob Job);

public sealed record SourceProcessingWork(Source Source, SourceVersion Version, SourceIngestionJob Job, IReadOnlyList<SourceChunk> ExistingChunks);

public enum SourceSaveOutcome
{
    Saved,
    Discarded
}
