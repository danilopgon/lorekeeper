using Api.Modules.Campaigns.Application;
using Api.Modules.Campaigns.Application.ListCampaigns;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Modules.Campaigns.Presentation;

internal static class ListCampaignsEndpoint
{
    public static void MapListCampaignEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", ListCampaigns)
            .WithName("ListCampaigns")
            .WithSummary("List campaigns")
            .WithDescription("Returns the campaigns available to the single operator.")
            .Produces<IReadOnlyList<CampaignDto>>(StatusCodes.Status200OK);
    }

    private static async Task<Ok<IReadOnlyList<CampaignDto>>> ListCampaigns(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var campaigns = await sender.Send(new ListCampaignsQuery(), cancellationToken);
        return TypedResults.Ok(campaigns);
    }
}
