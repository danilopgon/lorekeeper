using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.RetrySource;

public sealed class RetrySourceHandler(
    SourceLookup lookup,
    ISourceRepository sources,
    ISourceQueries sourceQueries,
    TimeProvider timeProvider) : IRequestHandler<RetrySourceCommand, Result<SourceDto>>
{
    public const string RetryUnavailableCode = "source_retry_unavailable";

    public async Task<Result<SourceDto>> Handle(RetrySourceCommand command, CancellationToken cancellationToken)
    {
        var key = await lookup.FindSourceKeyAsync(command.CampaignId, command.SourceId, cancellationToken);
        if (!key.IsSuccess)
        {
            return key.Error!;
        }

        var locked = await sources.LockExistingAsync(key.Value, cancellationToken);
        if (!locked.IsSuccess)
        {
            return locked.Error!;
        }

        if (locked.Value.Pending?.Job is not { State: IngestionJobState.Failed } failedJob)
        {
            await sources.ReleaseAsync(cancellationToken);
            return Error.Conflict(RetryUnavailableCode, "The source has no failed version to retry.");
        }

        failedJob.Requeue(timeProvider.GetUtcNow());
        await sources.SaveChangesAsync(cancellationToken);

        return await sourceQueries.ReadBackAsync(key.Value, cancellationToken);
    }
}

public sealed record RetrySourceCommand(string CampaignId, string SourceId) : IRequest<Result<SourceDto>>;
