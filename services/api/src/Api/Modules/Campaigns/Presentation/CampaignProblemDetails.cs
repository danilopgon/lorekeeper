using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Modules.Campaigns.Presentation;

internal static class CampaignProblemDetails
{
    public static ObjectResult InvalidName() => Create(
        StatusCodes.Status400BadRequest,
        "campaign_name_invalid",
        "Campaign name is invalid.",
        new Dictionary<string, object?> { ["errors"] = new Dictionary<string, string[]> { ["name"] = ["campaign_name_invalid"] } });

    public static ObjectResult NameConflict() => Create(
        StatusCodes.Status409Conflict,
        "campaign_name_conflict",
        "Campaign name already exists.",
        new Dictionary<string, object?> { ["errors"] = new Dictionary<string, string[]> { ["name"] = ["campaign_name_conflict"] } });

    public static ObjectResult InvalidId() => Create(
        StatusCodes.Status400BadRequest,
        "campaign_id_invalid",
        "Campaign id is invalid.");

    public static ObjectResult NotFound() => Create(
        StatusCodes.Status404NotFound,
        "campaign_not_found",
        "Campaign was not found.");

    private static ObjectResult Create(
        int statusCode,
        string code,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail
        };

        problem.Extensions["code"] = code;

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problem.Extensions[extension.Key] = extension.Value;
            }
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
