using Api.Common.Presentation;
using Api.Modules.Sources.Application.PasteSource;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Sources.Presentation;

internal static class SourceProblemDetails
{
    public static ObjectResult CampaignIdInvalid() =>
        ProblemResults.Create(StatusCodes.Status400BadRequest, "campaign_id_invalid", "Campaign id is invalid.");

    public static ObjectResult CampaignNotFound() =>
        ProblemResults.Create(StatusCodes.Status404NotFound, "campaign_not_found", "Campaign was not found.");

    public static ObjectResult Invalid(IReadOnlyList<SourceFieldError> errors) =>
        ProblemResults.Create(
            StatusCodes.Status400BadRequest,
            errors[0].Code,
            "Source content or metadata is invalid.",
            new Dictionary<string, object?>
            {
                ["errors"] = errors.GroupBy(error => error.Field).ToDictionary(group => group.Key, group => group.Select(error => error.Code).ToArray())
            });
}
