using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.GetCampaign;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Modules.Campaigns.Presentation;

internal static class GetCampaignEndpoint
{
    public static void MapGetCampaignEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{campaignId}", GetCampaign)
            .WithName("GetCampaign")
            .WithSummary("Get a campaign")
            .WithDescription("Returns one campaign by canonical UUID.")
            .Produces<CampaignDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
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
            GetCampaignStatus.InvalidId => CampaignProblemResults.Problem(StatusCodes.Status400BadRequest, CampaignProblemResults.IdInvalidCode, "Campaign id is invalid."),
            GetCampaignStatus.NotFound => CampaignProblemResults.Problem(StatusCodes.Status404NotFound, CampaignProblemResults.NotFoundCode, "Campaign was not found."),
            _ => throw new InvalidOperationException($"Unexpected get campaign status '{result.Status}'.")
        };
    }
}
