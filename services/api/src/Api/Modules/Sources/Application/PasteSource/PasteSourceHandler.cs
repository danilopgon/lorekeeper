using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Api.Modules.Sources.Domain;

namespace Api.Modules.Sources.Application.PasteSource;

public sealed class PasteSourceHandler(
    ICampaignQueries campaigns,
    ISourceRepository sources,
    ISourceQueries sourceQueries,
    TimeProvider timeProvider)
{
    public async Task<PasteSourceResult> Handle(PasteSourceCommand command, CancellationToken cancellationToken)
    {
        var campaignId = CampaignIdParser.Parse(command.CampaignId);
        if (!campaignId.IsSuccess)
        {
            return PasteSourceResult.CampaignIdInvalid();
        }

        if (await campaigns.GetByIdAsync(campaignId.Value, cancellationToken) is null)
        {
            return PasteSourceResult.CampaignNotFound();
        }

        var title = SourceTitle.Create(command.Title);
        var text = SourceText.Create(command.Text);
        var category = SourceClassification.ParseCategory(command.Category);
        var knowledgeStatus = SourceClassification.ParseKnowledgeStatus(command.KnowledgeStatus);

        var errors = new List<SourceFieldError>();
        if (!title.IsSuccess) errors.Add(new("title", title.ErrorCode!));
        if (!text.IsSuccess) errors.Add(new("text", text.ErrorCode!));
        if (!category.IsSuccess) errors.Add(new("category", category.ErrorCode!));
        if (!knowledgeStatus.IsSuccess) errors.Add(new("knowledgeStatus", knowledgeStatus.ErrorCode!));
        if (errors.Count > 0)
        {
            return PasteSourceResult.Invalid(errors);
        }

        var now = timeProvider.GetUtcNow();
        var source = Source.Create(campaignId.Value, title.Value!, SourceOrigin.Paste, category.Value, knowledgeStatus.Value, now);
        var version = SourceVersion.Create(source, text.Value!, fileName: null, now);
        sources.Add(source, version, SourceIngestionJob.Create(version, now));
        await sources.SaveChangesAsync(cancellationToken);

        var dto = await sourceQueries.GetAsync(source.CampaignId, source.Id, cancellationToken)
            ?? throw new InvalidOperationException("A pasted source must be readable after it is saved.");

        return PasteSourceResult.Accepted(dto);
    }
}

public sealed record PasteSourceCommand(string CampaignId, string? Title, string? Text, string? Category, string? KnowledgeStatus);

public sealed record SourceFieldError(string Field, string Code);

public sealed record PasteSourceResult(PasteSourceStatus Status, SourceDto? Source, IReadOnlyList<SourceFieldError> Errors)
{
    public static PasteSourceResult Accepted(SourceDto source) => new(PasteSourceStatus.Accepted, source, []);

    public static PasteSourceResult Invalid(IReadOnlyList<SourceFieldError> errors) => new(PasteSourceStatus.Invalid, null, errors);

    public static PasteSourceResult CampaignIdInvalid() => new(PasteSourceStatus.CampaignIdInvalid, null, []);

    public static PasteSourceResult CampaignNotFound() => new(PasteSourceStatus.CampaignNotFound, null, []);
}

public enum PasteSourceStatus
{
    Accepted,
    Invalid,
    CampaignIdInvalid,
    CampaignNotFound
}
