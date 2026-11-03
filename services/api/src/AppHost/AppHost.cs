var builder = DistributedApplication.CreateBuilder(args);

var dbUser = builder.AddParameter("postgres-user", "lorekeeper");
var dbPassword = builder.AddParameter("postgres-password", "lorekeeper_dev_password", secret: true);

var postgres = builder.AddPostgres("postgres", dbUser, dbPassword)
    .WithImageTag("17")
    .WithDataVolume("lorekeeper_postgres-data");
var campaigns = postgres.AddDatabase("Campaigns", "lorekeeper");

var migrate = builder.AddExecutable("migrate", "dotnet", "../../../..",
        "tool", "run", "dotnet-ef", "database", "update",
        "--project", "services/api/src/Api/Api.csproj",
        "--startup-project", "services/api/src/Api/Api.csproj")
    .WithReference(campaigns)
    .WaitFor(campaigns);

var api = builder.AddProject<Projects.Api>("api", "http")
    .WithReference(campaigns)
    .WaitForCompletion(migrate);

builder.AddJavaScriptApp("web", "../../../../apps/web", "start")
    .WithPnpm()
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WaitFor(api);

builder.Build().Run();
