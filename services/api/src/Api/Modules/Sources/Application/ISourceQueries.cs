namespace Api.Modules.Sources.Application;

public interface ISourceQueries
{
    Task<IReadOnlyList<SourceDto>> ListAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<SourceDto?> GetAsync(Guid campaignId, Guid sourceId, CancellationToken cancellationToken);
}
