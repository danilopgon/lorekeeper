using Api.Modules.Sources.Application;
using Api.Modules.Sources.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Modules.Sources.Infrastructure.Persistence;

public sealed class SourceRepository(SourcesDbContext dbContext) : ISourceRepository, IAsyncDisposable
{
    private readonly List<Func<CancellationToken, Task>> _deletesAfterSave = [];
    private IDbContextTransaction? _transaction;

    public void Add(Source source, SourceVersion version, SourceIngestionJob job)
    {
        dbContext.Sources.Add(source);
        AddVersion(version, job);
    }

    public async Task<LockedSource?> LockAsync(Guid campaignId, Guid sourceId, CancellationToken cancellationToken)
    {
        var source = await LockLiveSourceAsync(campaignId, sourceId, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var pending = await (
                from version in dbContext.Versions
                join job in dbContext.Jobs on new { version.Id, version.CampaignId } equals new { Id = job.SourceVersionId, job.CampaignId }
                where version.SourceId == source.Id
                      && version.CampaignId == campaignId
                      && version.Id != source.PublishedVersionId
                      && job.State != IngestionJobState.Succeeded
                orderby version.CreatedAt descending
                select new { version, job })
            .FirstOrDefaultAsync(cancellationToken);

        var publishedHash = source.PublishedVersionId is { } publishedId
            ? await dbContext.Versions
                .Where(version => version.Id == publishedId && version.CampaignId == campaignId)
                .Select(version => version.ContentHash)
                .SingleAsync(cancellationToken)
            : null;

        return new LockedSource(source, pending is null ? null : new PendingVersion(pending.version, pending.job), publishedHash);
    }

    public void AddVersion(SourceVersion version, SourceIngestionJob job)
    {
        dbContext.Versions.Add(version);
        dbContext.Jobs.Add(job);
    }

    public void DeleteVersion(Guid campaignId, Guid versionId) =>
        _deletesAfterSave.Add(cancellationToken => dbContext.Versions
            .Where(version => version.Id == versionId && version.CampaignId == campaignId)
            .ExecuteDeleteAsync(cancellationToken));

    public void DeleteAllVersions(Source source) =>
        _deletesAfterSave.Add(cancellationToken => dbContext.Versions
            .Where(version => version.SourceId == source.Id && version.CampaignId == source.CampaignId)
            .ExecuteDeleteAsync(cancellationToken));

    public async Task<SourceProcessingWork?> GetProcessingWorkAsync(Guid campaignId, Guid jobId, CancellationToken cancellationToken)
    {
        var sourceId = await (
                from job in dbContext.Jobs.AsNoTracking()
                join version in dbContext.Versions.AsNoTracking()
                    on new { Id = job.SourceVersionId, job.CampaignId } equals new { version.Id, version.CampaignId }
                where job.Id == jobId && job.CampaignId == campaignId
                select (Guid?)version.SourceId)
            .SingleOrDefaultAsync(cancellationToken);
        if (sourceId is null)
        {
            return null;
        }

        // The lock comes before the job is read again, so a removal or update that committed first is seen.
        var source = await LockLiveSourceAsync(campaignId, sourceId.Value, cancellationToken);
        if (source is null)
        {
            return null;
        }

        var claimedJob = await dbContext.Jobs.SingleOrDefaultAsync(job => job.Id == jobId && job.CampaignId == campaignId, cancellationToken);
        if (claimedJob is null)
        {
            return null;
        }

        var claimedVersion = await dbContext.Versions.SingleAsync(
            version => version.Id == claimedJob.SourceVersionId && version.CampaignId == campaignId,
            cancellationToken);
        var existingChunks = await dbContext.Chunks
            .Where(chunk => chunk.SourceVersionId == claimedVersion.Id && chunk.CampaignId == campaignId)
            .ToListAsync(cancellationToken);

        return new SourceProcessingWork(source, claimedVersion, claimedJob, existingChunks);
    }

    public void ReplaceChunks(SourceProcessingWork work, IEnumerable<SourceChunk> chunks)
    {
        dbContext.Chunks.RemoveRange(work.ExistingChunks);
        dbContext.Chunks.AddRange(chunks);
    }

    public async Task<SourceSaveOutcome> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await EndTransactionAsync(commit: false, cancellationToken);
            return SourceSaveOutcome.Discarded;
        }

        foreach (var delete in _deletesAfterSave)
        {
            await delete(cancellationToken);
        }

        _deletesAfterSave.Clear();
        await EndTransactionAsync(commit: true, cancellationToken);
        return SourceSaveOutcome.Saved;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>Opens the transaction that holds the row lock; <c>FOR UPDATE</c> outside one would release it at once.</summary>
    private async Task<Source?> LockLiveSourceAsync(Guid campaignId, Guid sourceId, CancellationToken cancellationToken)
    {
        _transaction ??= await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var locked = await dbContext.Sources
            .FromSql($"SELECT * FROM sources WHERE id = {sourceId} AND campaign_id = {campaignId} AND removed_at IS NULL FOR UPDATE")
            .ToListAsync(cancellationToken);

        return locked.SingleOrDefault();
    }

    private async Task EndTransactionAsync(bool commit, CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            return;
        }

        if (commit)
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
