using Api.Common.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Campaigns.Presentation;

internal static class CampaignProblemDetails
{
    public static ObjectResult InvalidName() =>
        ProblemResults.FieldError(StatusCodes.Status400BadRequest, "campaign_name_invalid", "Campaign name is invalid.", "name");

    public static ObjectResult NameConflict() =>
        ProblemResults.FieldError(StatusCodes.Status409Conflict, "campaign_name_conflict", "Campaign name already exists.", "name");

    public static ObjectResult InvalidId() =>
        ProblemResults.Create(StatusCodes.Status400BadRequest, "campaign_id_invalid", "Campaign id is invalid.");

    public static ObjectResult NotFound() =>
        ProblemResults.Create(StatusCodes.Status404NotFound, "campaign_not_found", "Campaign was not found.");
}
