using Api.Modules.Campaigns.Application;
using Api.Modules.Sources.Domain;
using Api.SharedKernel;
using FluentValidation;
using MediatR;

namespace Api.Modules.Sources.Application.UpdateSourceContent;

public sealed class UpdateSourceContentHandler(
    IValidator<UpdateSourceContentCommand> validator,
    ICampaignQueries campaigns,
    ISourceRepository sources,
    ISourceQueries sourceQueries,
    TimeProvider timeProvider) : IRequestHandler<UpdateSourceContentCommand, Result<SourceContentUpdateDto>>
{
    public const string ProcessingCode = "source_processing";

    /// <summary>Checks in the order documented in `06`: route, shape, existence, content, overlap, then identical content.</summary>
    public async Task<Result<SourceContentUpdateDto>> Handle(UpdateSourceContentCommand command, CancellationToken cancellationToken)
    {
        var key = await SourceRoute.ResolveAsync(campaigns, command.CampaignId, command.SourceId, cancellationToken);
        if (!key.IsSuccess)
        {
            return key.Error!;
        }

        var shape = await validator.ValidateAsync(command, cancellationToken);
        if (!shape.IsValid)
        {
            return Error.Validation(shape.Errors[0].ErrorCode, shape.Errors[0].ErrorMessage);
        }

        var locked = await sources.LockAsync(key.Value.CampaignId, key.Value.SourceId, cancellationToken);
        if (locked is null)
        {
            return SourceIdParser.NotFound;
        }

        var content = ReadContent(command);
        if (!content.IsSuccess)
        {
            return content.Error!;
        }

        if (locked.Pending?.Job.IsActive == true)
        {
            return Error.Conflict(ProcessingCode, "The source already has a version waiting to be processed.");
        }

        if (locked.PublishedContentHash == content.Value.Text.ContentHash)
        {
            return new SourceContentUpdateDto(await ReadBackAsync(key.Value, cancellationToken), Queued: false);
        }

        var source = locked.Source;
        var now = timeProvider.GetUtcNow();
        var version = content.Value.Upload is { } upload
            ? SourceVersion.FromFile(source, upload, now)
            : SourceVersion.Create(source, content.Value.Text, fileName: null, now);

        if (locked.Pending is { } failed)
        {
            sources.DeleteVersion(source.CampaignId, failed.Version.Id);
        }

        source.ReceiveContentUpdate(now);
        sources.AddVersion(version, SourceIngestionJob.Create(version, now));
        await sources.SaveChangesAsync(cancellationToken);

        return new SourceContentUpdateDto(await ReadBackAsync(key.Value, cancellationToken), Queued: true);
    }

    private static Result<NewContent> ReadContent(UpdateSourceContentCommand command)
    {
        if (command.File is { } file)
        {
            var upload = SourceUpload.Read(file.FileName, file.Length, file.Content);
            return upload.IsSuccess ? new NewContent(upload.Value.Text, upload.Value) : upload.Error!.ForField("file");
        }

        var text = SourceText.Create(command.Text);
        return text.IsSuccess ? new NewContent(text.Value, Upload: null) : text.Error!.ForField("text");
    }

    private async Task<SourceDto> ReadBackAsync(SourceKey key, CancellationToken cancellationToken) =>
        await sourceQueries.GetAsync(key.CampaignId, key.SourceId, cancellationToken)
        ?? throw new InvalidOperationException("An updated source must be readable.");

    private sealed record NewContent(SourceText Text, SourceUpload? Upload);
}

/// <summary>Exactly one of <see cref="File"/> and <see cref="Text"/> is set; <see cref="Text"/> is empty, not null, for an empty text field.</summary>
public sealed record UpdateSourceContentCommand(string CampaignId, string SourceId, UploadedFile? File, string? Text)
    : IRequest<Result<SourceContentUpdateDto>>;

/// <summary><see cref="Queued"/> is false when the content equals the published version's and nothing changed.</summary>
public sealed record SourceContentUpdateDto(SourceDto Source, bool Queued);
