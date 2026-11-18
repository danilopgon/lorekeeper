using Api.Modules.Sources.Application.FindContentMatches;

namespace Api.Modules.Sources.Application;

public interface ISourceQueries
{
    Task<IReadOnlyList<SourceDto>> ListAsync(Guid campaignId, CancellationToken cancellationToken);

    Task<SourceDto?> GetAsync(Guid campaignId, Guid sourceId, CancellationToken cancellationToken);

    /// <summary>Non-removed sources of the campaign whose published or pending version has one of the hashes.</summary>
    Task<IReadOnlyList<ContentMatchDto>> FindContentMatchesAsync(Guid campaignId, IReadOnlyList<string> hashes, CancellationToken cancellationToken);
}
