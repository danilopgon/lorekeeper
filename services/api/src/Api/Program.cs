using Api.Modules.Campaigns;
using Api.Common.Presentation.ExceptionHandling;
using Api.Common.Presentation.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi(options => options.AddSchemaTransformer<ProblemDetailsSchemaTransformer>());
builder.Services.AddCampaignsModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Lorekeeper API v1"));
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

public partial class Program;
