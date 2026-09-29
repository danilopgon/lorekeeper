using Api.Modules.Campaigns.Application.CreateCampaign;
using Api.Modules.Campaigns.Infrastructure;
using MediatR;
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
}
