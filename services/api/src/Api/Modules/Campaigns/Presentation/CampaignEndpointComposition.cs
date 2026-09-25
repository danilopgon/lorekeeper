namespace Api.Modules.Campaigns.Presentation;

public static class CampaignEndpointComposition
{
    public static IEndpointRouteBuilder MapCampaignEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/campaigns")
            .WithTags("Campaigns");

        group.MapListCampaignEndpoint();
        group.MapCreateCampaignEndpoint();
        group.MapGetCampaignEndpoint();

        return endpoints;
    }
}
