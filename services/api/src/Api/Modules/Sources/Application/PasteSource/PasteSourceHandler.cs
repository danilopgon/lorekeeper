using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using MediatR;

namespace Api.Modules.Sources.Application.PasteSource;

public sealed class PasteSourceHandler(
    ICampaignQueries campaigns,
    ISourceRepository sources,
    ISourceQueries sourceQueries,
    TimeProvider timeProvider) : IRequestHandler<PasteSourceCommand, Result<SourceDto>>
{
    public async Task<Result<SourceDto>> Handle(PasteSourceCommand command, CancellationToken cancellationToken)
    {
        var campaignId = CampaignIdParser.Parse(command.CampaignId);
        if (!campaignId.IsSuccess)
        {
            return campaignId.Error!;
        }

        if (await campaigns.GetByIdAsync(campaignId.Value, cancellationToken) is null)
        {
            return CampaignIdParser.NotFound;
        }

        var metadata = SourceMetadata.Parse(command.Title, command.Category, command.KnowledgeStatus);
        var text = SourceText.Create(command.Text);
        IReadOnlyList<FieldError> errors = [.. metadata.FieldErrors("metadata"), .. text.FieldErrors("text")];
        if (errors.Count > 0)
        {
            return Error.InvalidFields("Source content or metadata is invalid.", errors);
        }

        var now = timeProvider.GetUtcNow();
        var source = Source.Create(campaignId.Value, metadata.Value, SourceOrigin.Paste, now);
        var version = SourceVersion.Create(source, text.Value, fileName: null, now);
        sources.Add(source, version, SourceIngestionJob.Create(version, now));
        await sources.SaveChangesAsync(cancellationToken);

        var dto = await sourceQueries.GetAsync(source.CampaignId, source.Id, cancellationToken)
            ?? throw new InvalidOperationException("A pasted source must be readable after it is saved.");

        return dto;
    }
}

public sealed record PasteSourceCommand(string CampaignId, string? Title, string? Text, string? Category, string? KnowledgeStatus)
    : IRequest<Result<SourceDto>>;
