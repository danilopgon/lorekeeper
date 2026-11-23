using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using FluentValidation;

namespace Api.Modules.Sources.Application;

public sealed record SourceKey(Guid CampaignId, Guid SourceId);

/// <summary>Resolves the ids of a Sources request; campaign errors come first, as on every Sources route (`06`).</summary>
public sealed class SourceLookup(ICampaignQueries campaigns)
{
    public async Task<Result<Guid>> FindCampaignAsync(string campaignId, CancellationToken cancellationToken)
    {
        var campaign = CampaignIdParser.Parse(campaignId);
        if (!campaign.IsSuccess)
        {
            return campaign.Error!;
        }

        return await campaigns.GetByIdAsync(campaign.Value, cancellationToken) is null
            ? CampaignIdParser.NotFound
            : campaign.Value;
    }

    public async Task<Result<SourceKey>> FindSourceKeyAsync(string campaignId, string sourceId, CancellationToken cancellationToken)
    {
        var campaign = await FindCampaignAsync(campaignId, cancellationToken);
        if (!campaign.IsSuccess)
        {
            return campaign.Error!;
        }

        var source = SourceIdParser.Parse(sourceId);
        return source.IsSuccess ? new SourceKey(campaign.Value, source.Value) : source.Error!;
    }
}

public static class SourceResults
{
    /// <summary>Locks a non-removed source of the campaign until the repository saves or releases; otherwise <c>source_not_found</c>.</summary>
    public static async Task<Result<LockedSource>> LockExistingAsync(this ISourceRepository sources, SourceKey key, CancellationToken cancellationToken) =>
        await sources.LockAsync(key.CampaignId, key.SourceId, cancellationToken) is { } locked
            ? locked
            : SourceIdParser.NotFound;

    /// <summary>
    /// Reads the response after the source lock is released, so a removal that commits in between makes it
    /// <c>source_not_found</c> rather than an unexpected error.
    /// </summary>
    public static async Task<Result<SourceDto>> ReadBackAsync(this ISourceQueries queries, SourceKey key, CancellationToken cancellationToken) =>
        await queries.GetAsync(key.CampaignId, key.SourceId, cancellationToken) is { } source
            ? source
            : SourceIdParser.NotFound;

    /// <summary>The first request-shape failure as an error (ADR-011), or null when the shape is valid.</summary>
    public static async Task<Error?> ShapeErrorAsync<T>(this IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var shape = await validator.ValidateAsync(request, cancellationToken);
        return shape.IsValid ? null : Error.Validation(shape.Errors[0].ErrorCode, shape.Errors[0].ErrorMessage);
    }
}
