using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Modules.Campaigns.Presentation;

internal static class CampaignProblemResults
{
    public const string NameInvalidCode = "campaign_name_invalid";
    public const string NameConflictCode = "campaign_name_conflict";
    public const string IdInvalidCode = "campaign_id_invalid";
    public const string NotFoundCode = "campaign_not_found";

    public static ProblemHttpResult NameProblem(int statusCode, string code, string detail) =>
        Problem(statusCode, code, detail, new Dictionary<string, object?> { ["errors"] = new Dictionary<string, string[]> { ["name"] = [code] } });

    public static ProblemHttpResult Problem(
        int statusCode,
        string code,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        var problem = TypedResults.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail,
            extensions: extensions ?? new Dictionary<string, object?>());
        problem.ProblemDetails.Extensions["code"] = code;
        return problem;
    }
}
