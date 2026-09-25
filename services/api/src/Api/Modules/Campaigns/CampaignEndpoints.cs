using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Modules.Campaigns;

public static class CampaignEndpoints
{
    public const string NameInvalidCode = "campaign_name_invalid";
    public const string NameConflictCode = "campaign_name_conflict";
    public const string IdInvalidCode = "campaign_id_invalid";
    public const string NotFoundCode = "campaign_not_found";

    public static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/campaigns")
            .WithTags("Campaigns");

        group.MapGet("/", ListCampaigns)
            .WithName("ListCampaigns")
            .WithSummary("List campaigns")
            .WithDescription("Returns the campaigns available to the single operator.")
            .Produces<IReadOnlyList<CampaignDto>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateCampaign)
            .WithName("CreateCampaign")
            .WithSummary("Create a campaign")
            .WithDescription("Creates one campaign and returns its canonical campaign representation.")
            .Produces<CampaignDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{campaignId}", GetCampaign)
            .WithName("GetCampaign")
            .WithSummary("Get a campaign")
            .WithDescription("Returns one campaign by canonical UUID.")
            .Produces<CampaignDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<CampaignDto>>> ListCampaigns(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var campaigns = await sender.Send(new ListCampaignsQuery(), cancellationToken);
        return TypedResults.Ok(campaigns);
    }

    private static async Task<Results<Created<CampaignDto>, ProblemHttpResult>> CreateCampaign(
        [FromBody] CreateCampaignHttpRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignCommand(request?.Name), cancellationToken);

        return result.Status switch
        {
            CreateCampaignStatus.Created => TypedResults.Created($"/api/campaigns/{result.Campaign!.Id}", result.Campaign),
            CreateCampaignStatus.InvalidName => NameProblem(StatusCodes.Status400BadRequest, NameInvalidCode, "Campaign name is invalid."),
            CreateCampaignStatus.NameConflict => NameProblem(StatusCodes.Status409Conflict, NameConflictCode, "Campaign name already exists."),
            _ => throw new InvalidOperationException($"Unexpected create campaign status '{result.Status}'.")
        };
    }

    private static async Task<Results<Ok<CampaignDto>, ProblemHttpResult>> GetCampaign(
        string campaignId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCampaignQuery(campaignId), cancellationToken);

        return result.Status switch
        {
            GetCampaignStatus.Found => TypedResults.Ok(result.Campaign!),
            GetCampaignStatus.InvalidId => Problem(StatusCodes.Status400BadRequest, IdInvalidCode, "Campaign id is invalid."),
            GetCampaignStatus.NotFound => Problem(StatusCodes.Status404NotFound, NotFoundCode, "Campaign was not found."),
            _ => throw new InvalidOperationException($"Unexpected get campaign status '{result.Status}'.")
        };
    }

    private static ProblemHttpResult NameProblem(int statusCode, string code, string detail) =>
        Problem(statusCode, code, detail, new Dictionary<string, object?> { ["errors"] = new Dictionary<string, string[]> { ["name"] = [code] } });

    private static ProblemHttpResult Problem(
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

    private sealed record CreateCampaignHttpRequest(string? Name);
}
