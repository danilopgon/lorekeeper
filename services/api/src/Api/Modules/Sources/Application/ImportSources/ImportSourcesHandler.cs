using System.Text.Json.Serialization;
using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Domain;
using Api.Modules.Sources.Domain;
using Api.Modules.Sources.Domain.Chunking;
using Api.SharedKernel;
using FluentValidation;
using MediatR;

namespace Api.Modules.Sources.Application.ImportSources;

public sealed class ImportSourcesHandler(
    IValidator<ImportSourcesCommand> validator,
    ICampaignQueries campaigns,
    ISourceRepository sources,
    ISourceQueries sourceQueries,
    TimeProvider timeProvider) : IRequestHandler<ImportSourcesCommand, Result<ImportSourcesResultDto>>
{
    public async Task<Result<ImportSourcesResultDto>> Handle(ImportSourcesCommand command, CancellationToken cancellationToken)
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

        var shape = await validator.ValidateAsync(command, cancellationToken);
        if (!shape.IsValid)
        {
            return Error.Validation(shape.Errors[0].ErrorCode, shape.Errors[0].ErrorMessage);
        }

        var now = timeProvider.GetUtcNow();
        var outcomes = command.Files
            .Select((file, index) => (file, index, outcome: Accept(campaignId.Value, file, command.Metadata![index], now)))
            .ToList();

        foreach (var (_, _, outcome) in outcomes.Where(item => item.outcome.IsSuccess))
        {
            sources.Add(outcome.Value.Source, outcome.Value.Version, outcome.Value.Job);
        }

        await sources.SaveChangesAsync(cancellationToken);

        var items = new List<ImportItemDto>(outcomes.Count);
        foreach (var (file, index, outcome) in outcomes)
        {
            items.Add(outcome.IsSuccess
                ? ImportItemDto.Accepted(index, file.FileName, await ReadBackAsync(outcome.Value.Source, cancellationToken))
                : ImportItemDto.Rejected(index, file.FileName, outcome.Error!.Code));
        }

        return new ImportSourcesResultDto(items);
    }

    /// <summary>Checks one file in the documented order: name, type, size, encoding, emptiness, then metadata.</summary>
    private static Result<AcceptedSource> Accept(Guid campaignId, ImportFile file, ImportMetadataEntry metadata, DateTimeOffset now)
    {
        if (!SourceFormats.IsStorableFileName(file.FileName))
        {
            return Error.Validation(SourceFormats.FileNameInvalidCode, "Source file name cannot be stored.");
        }

        var format = SourceFormats.FromFileName(file.FileName);
        if (!format.IsSuccess)
        {
            return format.Error!;
        }

        if (file.Length > SourceFormats.MaxFileBytes || file.Content is null)
        {
            return Error.Validation(SourceText.TooLargeCode, "Source file is larger than 1 MiB.");
        }

        var text = SourceText.Decode(file.Content);
        if (!text.IsSuccess)
        {
            return text.Error!;
        }

        if (format.Value == SourceFormat.Markdown && !MarkdownChunker.HasBody(text.Value.Value))
        {
            return Error.Validation(SourceText.EmptyCode, "Markdown file has no content besides its frontmatter.");
        }

        var parsed = SourceMetadata.Parse(metadata.Title, metadata.Category, metadata.KnowledgeStatus);
        if (!parsed.IsSuccess)
        {
            return parsed.Error!;
        }

        var source = Source.Create(campaignId, parsed.Value, SourceOrigin.File, now);
        var version = SourceVersion.FromFile(source, text.Value, file.FileName, format.Value, now);
        return new AcceptedSource(source, version, SourceIngestionJob.Create(version, now));
    }

    private async Task<SourceDto> ReadBackAsync(Source source, CancellationToken cancellationToken) =>
        await sourceQueries.GetAsync(source.CampaignId, source.Id, cancellationToken)
        ?? throw new InvalidOperationException("An imported source must be readable after it is saved.");

    private sealed record AcceptedSource(Source Source, SourceVersion Version, SourceIngestionJob Job);
}

/// <summary>An uploaded file already read by Presentation; <see cref="Content"/> is null when the file was too large to read.</summary>
public sealed record ImportFile(string FileName, long Length, byte[]? Content);

public sealed record ImportMetadataEntry(string? Title, string? Category, string? KnowledgeStatus);

/// <summary><see cref="Metadata"/> is null when the metadata part is missing or is not a JSON array of entries.</summary>
public sealed record ImportSourcesCommand(string CampaignId, IReadOnlyList<ImportFile> Files, IReadOnlyList<ImportMetadataEntry>? Metadata)
    : IRequest<Result<ImportSourcesResultDto>>;

public sealed record ImportSourcesResultDto(IReadOnlyList<ImportItemDto> Items);

public sealed record ImportItemDto(int Index, string FileName, ImportOutcomeDto Outcome, SourceDto? Source, string? ErrorCode)
{
    public static ImportItemDto Accepted(int index, string fileName, SourceDto source) => new(index, fileName, ImportOutcomeDto.Accepted, source, null);

    public static ImportItemDto Rejected(int index, string fileName, string errorCode) => new(index, fileName, ImportOutcomeDto.Rejected, null, errorCode);
}

[JsonConverter(typeof(JsonStringEnumConverter<ImportOutcomeDto>))]
public enum ImportOutcomeDto
{
    [JsonStringEnumMemberName("accepted")] Accepted,
    [JsonStringEnumMemberName("rejected")] Rejected
}
