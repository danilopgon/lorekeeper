using Api.Modules.Campaigns.Presentation;
using Api.Presentation.Core;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unit.Modules.Campaigns.Presentation;

public sealed class CampaignsControllerTests
{
    [Fact]
    public void MapsVerbOrientedCampaignMvcActionsWithTheirEstablishedRouteNamesAndOpenApiDocumentation()
    {
        using var app = CreateApplication();

        app.MapControllers();

        AssertCampaignControllerRoutes(GetRouteEndpoints(app));
    }

    [Fact]
    public async Task ApiRegistersAnExceptionHandlerForUnexpectedFailures()
    {
        var problemDetailsService = new RecordingProblemDetailsService();
        var handler = new UnexpectedExceptionHandler(
            NullLogger<UnexpectedExceptionHandler>.Instance,
            problemDetailsService);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("failure"), CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetailsService.WrittenContext!.ProblemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetailsService.WrittenContext.ProblemDetails.Extensions["code"].Should().Be("unexpected_error");
    }

    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(CampaignsGetController).Assembly);
        builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(CampaignsGetController).Assembly));
        return builder.Build();
    }

    private static Dictionary<string, RouteEndpoint> GetRouteEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToDictionary(static endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);

    private static void AssertCampaignControllerRoutes(IReadOnlyDictionary<string, RouteEndpoint> routeEndpoints)
    {
        routeEndpoints.Keys.Should().BeEquivalentTo("ListCampaigns", "CreateCampaign", "GetCampaign");
        routeEndpoints["ListCampaigns"].RoutePattern.RawText.Should().Be("api/campaigns");
        routeEndpoints["CreateCampaign"].RoutePattern.RawText.Should().Be("api/campaigns");
        routeEndpoints["GetCampaign"].RoutePattern.RawText.Should().Be("api/campaigns/{campaignId}");
        routeEndpoints.Values.Should().OnlyContain(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() != null);
        routeEndpoints["ListCampaigns"].Metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary.Should().Be("List campaigns");
        routeEndpoints["ListCampaigns"].Metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description.Should().Be("Returns the campaigns available to the single operator.");
        routeEndpoints["CreateCampaign"].Metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary.Should().Be("Create a campaign");
        routeEndpoints["CreateCampaign"].Metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description.Should().Be("Creates one campaign and returns its canonical campaign representation.");
        routeEndpoints["GetCampaign"].Metadata.GetMetadata<IEndpointSummaryMetadata>()!.Summary.Should().Be("Get a campaign");
        routeEndpoints["GetCampaign"].Metadata.GetMetadata<IEndpointDescriptionMetadata>()!.Description.Should().Be("Returns one campaign by canonical UUID.");
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? WrittenContext { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            WrittenContext = context;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            WrittenContext = context;
            return ValueTask.CompletedTask;
        }
    }
}
