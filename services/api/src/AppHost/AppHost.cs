var builder = DistributedApplication.CreateBuilder(args);

var dbUser = builder.AddParameter("postgres-user", "lorekeeper");
var dbPassword = builder.AddParameter("postgres-password", "lorekeeper_dev_password", secret: true);

var postgres = builder.AddPostgres("postgres", dbUser, dbPassword)
    .WithImageTag("17")
    .WithDataVolume("lorekeeper_postgres-data");
var campaigns = postgres.AddDatabase("Campaigns", "lorekeeper");

var migrateCampaigns = AddMigration("migrate-campaigns", "CampaignsDbContext")
    .WaitFor(campaigns);
var migrateSources = AddMigration("migrate-sources", "SourcesDbContext")
    .WaitForCompletion(migrateCampaigns);

var api = builder.AddProject<Projects.Api>("api", "http")
    .WithReference(campaigns)
    .WaitForCompletion(migrateSources);

builder.AddJavaScriptApp("web", "../../../../apps/web", "start")
    .WithPnpm()
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WaitFor(api);

builder.Build().Run();

IResourceBuilder<ExecutableResource> AddMigration(string name, string context) =>
    builder.AddExecutable(name, "dotnet", "../../../..",
            "tool", "run", "dotnet-ef", "database", "update",
            "--context", context,
            "--project", "services/api/src/Api/Api.csproj",
            "--startup-project", "services/api/src/Api/Api.csproj")
        .WithReference(campaigns);
