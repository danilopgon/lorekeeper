using Api.Modules.Campaigns;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCampaignsModule(builder.Configuration);

var app = builder.Build();

app.UseCampaignProblemDetails();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapCampaignsModule();

app.Run();

public partial class Program;
