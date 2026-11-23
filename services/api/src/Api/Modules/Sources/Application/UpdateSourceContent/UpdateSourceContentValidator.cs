using FluentValidation;

namespace Api.Modules.Sources.Application.UpdateSourceContent;

/// <summary>Request shape only (ADR-011): the content itself is checked by the domain rules.</summary>
public sealed class UpdateSourceContentValidator : AbstractValidator<UpdateSourceContentCommand>
{
    public const string ContentPartInvalidCode = "source_content_part_invalid";

    public UpdateSourceContentValidator()
    {
        RuleFor(command => command)
            .Must(command => command.Files.Count + command.Texts.Count == 1)
            .WithErrorCode(ContentPartInvalidCode)
            .WithMessage("Send exactly one part: a single file or a single text field.");
    }
}
