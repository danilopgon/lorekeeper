using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Common.Presentation;

public static class ProblemResults
{
    public static ObjectResult Create(
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

    public static ObjectResult FieldError(int statusCode, string code, string detail, string field) =>
        Create(statusCode, code, detail, new Dictionary<string, object?> { ["errors"] = new Dictionary<string, string[]> { [field] = [code] } });
}
