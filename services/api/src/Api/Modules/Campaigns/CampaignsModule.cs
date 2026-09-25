using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Infrastructure;
using Api.Modules.Campaigns.Presentation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns;

public static class CampaignsModule
{
    public static IServiceCollection AddCampaignsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CampaignsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Campaigns")
                ?? throw new InvalidOperationException("Connection string 'Campaigns' is not configured.");
            options.UseNpgsql(connectionString);
        });

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<CreateCampaignHandler>());

        return services;
    }

    public static IApplicationBuilder UseCampaignProblemDetails(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred.",
                    Extensions = { ["code"] = "unexpected_error" }
                });
            }
        });
    }

    public static IEndpointRouteBuilder MapCampaignsModule(this IEndpointRouteBuilder endpoints) =>
        CampaignEndpointComposition.MapCampaignEndpoints(endpoints);
}
