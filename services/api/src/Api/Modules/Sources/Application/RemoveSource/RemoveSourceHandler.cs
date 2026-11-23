using Api.Modules.Campaigns.Application;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.RemoveSource;

/// <summary>Removal wins over processing (`03`): the tombstone and the deletion of every version commit together.</summary>
public sealed class RemoveSourceHandler(
    ICampaignQueries campaigns,
    ISourceRepository sources,
    TimeProvider timeProvider) : IRequestHandler<RemoveSourceCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RemoveSourceCommand command, CancellationToken cancellationToken)
    {
        var key = await SourceRoute.ResolveAsync(campaigns, command.CampaignId, command.SourceId, cancellationToken);
        if (!key.IsSuccess)
        {
            return key.Error!;
        }

        var locked = await sources.LockAsync(key.Value.CampaignId, key.Value.SourceId, cancellationToken);
        if (locked is null)
        {
            return SourceIdParser.NotFound;
        }

        locked.Source.Remove(timeProvider.GetUtcNow());
        sources.DeleteAllVersions(locked.Source);
        await sources.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record RemoveSourceCommand(string CampaignId, string SourceId) : IRequest<Result<Unit>>;
