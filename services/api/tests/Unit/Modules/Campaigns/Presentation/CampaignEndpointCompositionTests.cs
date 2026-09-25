using Api.Modules.Campaigns;
using Api.Modules.Campaigns.Presentation;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Unit.Modules.Campaigns.Presentation;

public sealed class CampaignEndpointCompositionTests
{
    [Fact]
    public void MapsTheThreeCampaignUseCaseAdaptersWithTheirEstablishedRouteNames()
    {
        using var app = CreateApplication();

        CampaignEndpointComposition.MapCampaignEndpoints(app);

        AssertCampaignRoutes(GetRouteEndpoints(app));
    }

    [Fact]
    public void CampaignsModuleUsesThePresentationCompositionMapper()
    {
        using var app = CreateApplication();

        app.MapCampaignsModule();

        AssertCampaignRoutes(GetRouteEndpoints(app));
    }

    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(CampaignEndpointComposition).Assembly));
        return builder.Build();
    }

    private static Dictionary<string, RouteEndpoint> GetRouteEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToDictionary(static endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);

    private static void AssertCampaignRoutes(IReadOnlyDictionary<string, RouteEndpoint> routeEndpoints)
    {
        routeEndpoints.Keys.Should().BeEquivalentTo("ListCampaigns", "CreateCampaign", "GetCampaign");
        routeEndpoints["ListCampaigns"].RoutePattern.RawText.Should().Be("/api/campaigns/");
        routeEndpoints["CreateCampaign"].RoutePattern.RawText.Should().Be("/api/campaigns/");
        routeEndpoints["GetCampaign"].RoutePattern.RawText.Should().Be("/api/campaigns/{campaignId}");
    }
}
