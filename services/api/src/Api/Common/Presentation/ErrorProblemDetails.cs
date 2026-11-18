using Api.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Common.Presentation;

public static class ErrorProblemDetails
{
    public static ObjectResult ToProblem(this Error error)
    {
        var statusCode = error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => throw new InvalidOperationException($"Unmapped error kind '{error.Kind}'.")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = error.Message
        };

        problem.Extensions["code"] = error.Code;

        if (error.Fields.Count > 0)
        {
            problem.Extensions["errors"] = error.Fields
                .GroupBy(field => field.Field)
                .ToDictionary(group => group.Key, group => group.Select(field => field.Code).ToArray());
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
