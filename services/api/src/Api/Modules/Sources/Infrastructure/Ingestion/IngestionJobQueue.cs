using Api.Modules.Sources.Application.ProcessSourceVersion;
using Api.Modules.Sources.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Sources.Infrastructure.Ingestion;

public sealed class IngestionJobQueue(SourcesDbContext dbContext)
{
    public async Task<ClaimedIngestionJob?> ClaimNextAsync(DateTimeOffset now, TimeSpan lease, CancellationToken cancellationToken)
    {
        var claimed = await dbContext.Database.SqlQuery<ClaimedRow>(
            $"""
            UPDATE source_ingestion_jobs
            SET state = 'processing',
                attempts = attempts + 1,
                lease_expires_at = {now + lease},
                updated_at = {now}
            WHERE id = (
                SELECT id
                FROM source_ingestion_jobs
                WHERE (state = 'queued' AND next_attempt_at <= {now})
                   OR (state = 'processing' AND lease_expires_at < {now})
                ORDER BY next_attempt_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED)
            RETURNING id AS "JobId", campaign_id AS "CampaignId", source_version_id AS "SourceVersionId", attempts AS "Attempts"
            """).ToListAsync(cancellationToken);

        return claimed.Count == 0
            ? null
            : new ClaimedIngestionJob(claimed[0].JobId, claimed[0].CampaignId, claimed[0].SourceVersionId, claimed[0].Attempts);
    }

    public Task RecordFailureAsync(ClaimedIngestionJob job, IngestionFailureOutcome outcome, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var state = outcome.State == IngestionJobState.Failed ? "failed" : "queued";

        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE source_ingestion_jobs
            SET state = {state},
                next_attempt_at = COALESCE({outcome.NextAttemptAt}, next_attempt_at),
                lease_expires_at = NULL,
                error_code = {outcome.ErrorCode},
                updated_at = {now}
            WHERE id = {job.JobId} AND campaign_id = {job.CampaignId} AND state = 'processing' AND attempts = {job.Attempts}
            """,
            cancellationToken);
    }

    private sealed record ClaimedRow(Guid JobId, Guid CampaignId, Guid SourceVersionId, int Attempts);
}
