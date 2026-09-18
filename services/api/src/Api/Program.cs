using Api.Modules.Campaigns.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CampaignsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Campaigns")
        ?? throw new InvalidOperationException("Connection string 'Campaigns' is not configured.");
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
