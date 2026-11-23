using System.Text.RegularExpressions;
using Api.SharedKernel;
using FluentValidation;
using MediatR;

namespace Api.Modules.Sources.Application.FindContentMatches;

public sealed class FindContentMatchesHandler(
    IValidator<FindContentMatchesQuery> validator,
    SourceLookup lookup,
    ISourceQueries sources) : IRequestHandler<FindContentMatchesQuery, Result<ContentMatchesDto>>
{
    public async Task<Result<ContentMatchesDto>> Handle(FindContentMatchesQuery query, CancellationToken cancellationToken)
    {
        var campaignId = await lookup.FindCampaignAsync(query.CampaignId, cancellationToken);
        if (!campaignId.IsSuccess)
        {
            return campaignId.Error!;
        }

        if (await validator.ShapeErrorAsync(query, cancellationToken) is { } shapeError)
        {
            return shapeError;
        }

        var matches = await sources.FindContentMatchesAsync(campaignId.Value, query.Hashes!.Distinct().ToList(), cancellationToken);
        return new ContentMatchesDto(matches);
    }
}

public sealed record FindContentMatchesQuery(string CampaignId, IReadOnlyList<string>? Hashes) : IRequest<Result<ContentMatchesDto>>;

public sealed record ContentMatchesDto(IReadOnlyList<ContentMatchDto> Matches);

public sealed record ContentMatchDto(string Hash, string SourceId, string Title);

public sealed partial class FindContentMatchesValidator : AbstractValidator<FindContentMatchesQuery>
{
    public const int MaxHashes = 50;
    private const string InvalidCode = "source_hashes_invalid";
    private const string InvalidMessage = "Send 1 to 50 SHA-256 hashes as 64 lowercase hexadecimal characters.";

    public FindContentMatchesValidator()
    {
        RuleFor(query => query.Hashes)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode(InvalidCode)
            .WithMessage(InvalidMessage)
            .Must(hashes => hashes!.Count <= MaxHashes && hashes.All(hash => hash is not null && Sha256Hex().IsMatch(hash)))
            .WithErrorCode(InvalidCode)
            .WithMessage(InvalidMessage);
    }

    [GeneratedRegex(@"\A[0-9a-f]{64}\z")]
    private static partial Regex Sha256Hex();
}
