using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Application.GetCampaign;
using Api.Modules.Campaigns.Application.ListCampaigns;
using Api.Modules.Campaigns.Infrastructure;
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

        services.AddScoped<CreateCampaignHandler>();
        services.AddScoped<ListCampaignsHandler>();
        services.AddScoped<GetCampaignHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapCampaignsModule(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapCampaignEndpoints();
}
