using Api.Modules.Sources.Application;
using Api.Modules.Sources.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Sources.Infrastructure.Persistence;

public sealed class SourceRepository(SourcesDbContext dbContext) : ISourceRepository
{
    public void Add(Source source, SourceVersion version, SourceIngestionJob job)
    {
        dbContext.Sources.Add(source);
        dbContext.Versions.Add(version);
        dbContext.Jobs.Add(job);
    }

    public async Task<SourceProcessingWork?> GetProcessingWorkAsync(Guid campaignId, Guid jobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs.SingleOrDefaultAsync(job => job.Id == jobId && job.CampaignId == campaignId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var version = await dbContext.Versions.SingleOrDefaultAsync(
            version => version.Id == job.SourceVersionId && version.CampaignId == campaignId,
            cancellationToken);
        if (version is null)
        {
            return null;
        }

        var source = await dbContext.Sources.SingleAsync(
            source => source.Id == version.SourceId && source.CampaignId == campaignId,
            cancellationToken);
        var existingChunks = await dbContext.Chunks
            .Where(chunk => chunk.SourceVersionId == version.Id && chunk.CampaignId == campaignId)
            .ToListAsync(cancellationToken);

        return new SourceProcessingWork(source, version, job, existingChunks);
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
            return SourceSaveOutcome.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SourceSaveOutcome.Discarded;
        }
    }
}
