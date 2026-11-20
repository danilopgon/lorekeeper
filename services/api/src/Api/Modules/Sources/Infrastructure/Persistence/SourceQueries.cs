using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.FindContentMatches;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Sources.Infrastructure.Persistence;

public sealed class SourceQueries(SourcesDbContext dbContext) : ISourceQueries
{
    public async Task<IReadOnlyList<SourceDto>> ListAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var rows = await Rows(Live(campaignId)
                .OrderByDescending(source => source.CreatedAt)
                .ThenByDescending(source => source.Id))
            .ToListAsync(cancellationToken);

        return rows.Select(row => row.ToDto()).ToList();
    }

    public async Task<SourceDto?> GetAsync(Guid campaignId, Guid sourceId, CancellationToken cancellationToken)
    {
        var row = await Rows(Live(campaignId).Where(source => source.Id == sourceId)).SingleOrDefaultAsync(cancellationToken);
        return row?.ToDto();
    }

    public async Task<IReadOnlyList<ContentMatchDto>> FindContentMatchesAsync(Guid campaignId, IReadOnlyList<string> hashes, CancellationToken cancellationToken)
    {
        var rows = await (
                from version in dbContext.Versions.AsNoTracking()
                join source in Live(campaignId) on new { Id = version.SourceId, version.CampaignId } equals new { source.Id, source.CampaignId }
                where version.CampaignId == campaignId && hashes.Contains(version.ContentHash)
                orderby source.CreatedAt descending, source.Id
                select new { version.ContentHash, source.Id, source.Title })
            .ToListAsync(cancellationToken);

        return rows
            .DistinctBy(row => (row.ContentHash, row.Id))
            .Select(row => new ContentMatchDto(row.ContentHash, CanonicalUuid.Format(row.Id), row.Title.Value))
            .ToList();
    }

    private IQueryable<Source> Live(Guid campaignId) =>
        dbContext.Sources.AsNoTracking().Where(source => source.CampaignId == campaignId && source.RemovedAt == null);

    private IQueryable<SourceRow> Rows(IQueryable<Source> sources) =>
        from source in sources
        select new SourceRow(
            source.Id,
            source.CampaignId,
            source.Title,
            source.Origin,
            source.Category,
            source.KnowledgeStatus,
            source.PublishedVersionId != null,
            source.LastPublishedAt,
            (from job in dbContext.Jobs
             join version in dbContext.Versions on new { Id = job.SourceVersionId, job.CampaignId } equals new { version.Id, version.CampaignId }
             where version.SourceId == source.Id
                   && version.CampaignId == source.CampaignId
                   && version.Id != source.PublishedVersionId
                   && job.State != IngestionJobState.Succeeded
             orderby job.CreatedAt descending
             select new PendingJobRow(job.State, job.ErrorCode, job.Attempts)).FirstOrDefault(),
            source.CreatedAt,
            source.UpdatedAt);

    private sealed record PendingJobRow(IngestionJobState State, string? ErrorCode, int Attempts);

    private sealed record SourceRow(
        Guid Id,
        Guid CampaignId,
        SourceTitle Title,
        SourceOrigin Origin,
        SourceCategory? Category,
        KnowledgeStatus KnowledgeStatus,
        bool IsPublished,
        DateTimeOffset? LastPublishedAt,
        PendingJobRow? PendingJob,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        public SourceDto ToDto() => new(
            CanonicalUuid.Format(Id),
            CanonicalUuid.Format(CampaignId),
            Title.Value,
            Origin switch
            {
                SourceOrigin.File => SourceOriginDto.File,
                _ => SourceOriginDto.Paste
            },
            Category switch
            {
                null => null,
                SourceCategory.SessionNotes => SourceCategoryDto.SessionNotes,
                SourceCategory.Preparation => SourceCategoryDto.Preparation,
                SourceCategory.CharactersPlaces => SourceCategoryDto.CharactersPlaces,
                _ => SourceCategoryDto.GeneralReference
            },
            KnowledgeStatus switch
            {
                KnowledgeStatus.Preparation => KnowledgeStatusDto.Preparation,
                KnowledgeStatus.Occurred => KnowledgeStatusDto.Occurred,
                _ => KnowledgeStatusDto.Unknown
            },
            IsPublished ? SourceAvailabilityDto.Available : SourceAvailabilityDto.Unavailable,
            LastPublishedAt,
            PendingJob is null
                ? null
                : new SourceProcessingDto(
                    PendingJob.State switch
                    {
                        IngestionJobState.Queued => SourceProcessingStateDto.Queued,
                        IngestionJobState.Processing => SourceProcessingStateDto.Processing,
                        _ => SourceProcessingStateDto.Failed
                    },
                    PendingJob.State == IngestionJobState.Failed ? PendingJob.ErrorCode : null,
                    PendingJob.Attempts),
            CreatedAt,
            UpdatedAt);
    }
}
