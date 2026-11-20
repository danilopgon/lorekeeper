using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Common.Presentation;

/// <summary>
/// Reads the form before model binding so a body over the action's size limits answers 413.
/// Without it, MVC turns the limit failure into a model-state error and the action answers 400.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RejectOversizedFormAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (request.HasFormContentType)
        {
            try
            {
                await request.ReadFormAsync(context.HttpContext.RequestAborted);
            }
            catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
            {
                context.Result = TooLarge();
                return;
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException)
            {
                // A malformed body is not a size problem: model binding reports it as 400.
            }
        }

        await next();
    }

    private static ObjectResult TooLarge()
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status413PayloadTooLarge),
            Detail = "The request body is larger than this endpoint accepts."
        };

        return new ObjectResult(problem) { StatusCode = StatusCodes.Status413PayloadTooLarge };
    }
}
