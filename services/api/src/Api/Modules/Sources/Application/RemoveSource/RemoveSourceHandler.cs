using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.RemoveSource;

/// <summary>Removal wins over processing (`03`): the tombstone and the deletion of every version commit together.</summary>
public sealed class RemoveSourceHandler(
    SourceLookup lookup,
    ISourceRepository sources,
    TimeProvider timeProvider) : IRequestHandler<RemoveSourceCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RemoveSourceCommand command, CancellationToken cancellationToken)
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

        var source = locked.Value.Source;
        source.Remove(timeProvider.GetUtcNow());
        sources.DeleteAllVersions(source);
        await sources.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public sealed record RemoveSourceCommand(string CampaignId, string SourceId) : IRequest<Result<Unit>>;
