using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.CreateCampaign;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Campaigns.Presentation;

internal static class CreateCampaignEndpoint
{
    public static void MapCreateCampaignEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/", CreateCampaign)
            .WithName("CreateCampaign")
            .WithSummary("Create a campaign")
            .WithDescription("Creates one campaign and returns its canonical campaign representation.")
            .Produces<CampaignDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Created<CampaignDto>, ProblemHttpResult>> CreateCampaign(
        [FromBody] CreateCampaignRequest? request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCampaignCommand(request?.Name), cancellationToken);

        return result.Status switch
        {
            CreateCampaignStatus.Created => TypedResults.Created($"/api/campaigns/{result.Campaign!.Id}", result.Campaign),
            CreateCampaignStatus.InvalidName => CampaignProblemResults.NameProblem(StatusCodes.Status400BadRequest, CampaignProblemResults.NameInvalidCode, "Campaign name is invalid."),
            CreateCampaignStatus.NameConflict => CampaignProblemResults.NameProblem(StatusCodes.Status409Conflict, CampaignProblemResults.NameConflictCode, "Campaign name already exists."),
            _ => throw new InvalidOperationException($"Unexpected create campaign status '{result.Status}'.")
        };
    }

    private sealed record CreateCampaignRequest(string? Name);
}
