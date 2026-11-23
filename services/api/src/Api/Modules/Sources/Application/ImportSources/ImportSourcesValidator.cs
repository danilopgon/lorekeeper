using FluentValidation;

namespace Api.Modules.Sources.Application.ImportSources;

/// <summary>Request shape only (ADR-011): per-file content and metadata rules are item outcomes, not request errors.</summary>
public sealed class ImportSourcesValidator : AbstractValidator<ImportSourcesCommand>
{
    public const int MaxFiles = 50;

    public ImportSourcesValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(command => command.Files)
            .NotEmpty()
            .WithErrorCode("source_batch_empty")
            .WithMessage("Select at least one file.");

        RuleFor(command => command.Files.Count)
            .LessThanOrEqualTo(MaxFiles)
            .WithErrorCode("source_batch_too_large")
            .WithMessage($"Import at most {MaxFiles} files at once.");

        RuleFor(command => command.Metadata)
            .Must((command, metadata) => metadata is not null && metadata.Count == command.Files.Count && metadata.All(entry => entry is not null))
            .WithErrorCode("source_metadata_invalid")
            .WithMessage("The metadata part must be a JSON array with one entry per file.");
    }
}
