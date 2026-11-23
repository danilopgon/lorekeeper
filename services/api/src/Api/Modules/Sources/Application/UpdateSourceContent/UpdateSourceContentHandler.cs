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
            await sources.ReleaseAsync(cancellationToken);
            return content.Error!;
        }

        if (locked.Pending?.Job.IsActive == true)
        {
            await sources.ReleaseAsync(cancellationToken);
            return Error.Conflict(ProcessingCode, "The source already has a version waiting to be processed.");
        }

        if (locked.PublishedContentHash == content.Value.Text.ContentHash)
        {
            await sources.ReleaseAsync(cancellationToken);
            return await RespondAsync(key.Value, queued: false, cancellationToken);
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

        return await RespondAsync(key.Value, queued: true, cancellationToken);
    }

    private async Task<Result<SourceContentUpdateDto>> RespondAsync(SourceKey key, bool queued, CancellationToken cancellationToken)
    {
        var source = await sourceQueries.ReadBackAsync(key, cancellationToken);
        return source.IsSuccess ? new SourceContentUpdateDto(source.Value, queued) : source.Error!;
    }

    private static Result<NewContent> ReadContent(UpdateSourceContentCommand command)
    {
        if (command.Files is [var file])
        {
            var upload = SourceUpload.Read(file.FileName, file.Length, file.Content);
            return upload.IsSuccess ? new NewContent(upload.Value.Text, upload.Value) : upload.Error!.ForField("file");
        }

        var text = SourceText.Create(command.Texts.Single());
        return text.IsSuccess ? new NewContent(text.Value, Upload: null) : text.Error!.ForField("text");
    }

    private sealed record NewContent(SourceText Text, SourceUpload? Upload);
}

/// <summary>Every `file` and `text` part of the request; the validator requires exactly one part in total.</summary>
public sealed record UpdateSourceContentCommand(string CampaignId, string SourceId, IReadOnlyList<UploadedFile> Files, IReadOnlyList<string> Texts)
    : IRequest<Result<SourceContentUpdateDto>>;

public sealed record SourceContentUpdateDto(SourceDto Source, bool Queued);
