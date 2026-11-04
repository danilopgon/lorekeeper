using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration;

public class OpenApiEndpointsTests
{
    private const string DocumentPath = "/openapi/v1.json";
    private const string SwaggerUiPath = "/swagger/index.html";

    [Theory]
    [InlineData(DocumentPath)]
    [InlineData(SwaggerUiPath)]
    public async Task ContractSurfacesAreServedInDevelopment(string path)
    {
        using var response = await GetInEnvironment("Development", path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Production", DocumentPath)]
    [InlineData("Production", SwaggerUiPath)]
    [InlineData("Staging", DocumentPath)]
    [InlineData("Staging", SwaggerUiPath)]
    public async Task ContractSurfacesAreAbsentOutsideDevelopment(string environment, string path)
    {
        using var response = await GetInEnvironment(environment, path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/campaigns", "get", "200")]
    [InlineData("/api/campaigns", "post", "201")]
    [InlineData("/api/campaigns/{campaignId}", "get", "200")]
    public async Task DocumentDescribesCampaignSuccessResponses(string path, string method, string status)
    {
        var document = await ReadDocument();

        document["paths"]![path]![method]!["responses"]![status]!["content"]!["application/json"]
            .Should().NotBeNull();
    }

    [Theory]
    [InlineData("/api/campaigns", "post", "400")]
    [InlineData("/api/campaigns", "post", "409")]
    [InlineData("/api/campaigns/{campaignId}", "get", "400")]
    [InlineData("/api/campaigns/{campaignId}", "get", "404")]
    public async Task DocumentDeclaresProblemDetailsOnCampaignFailures(string path, string method, string status)
    {
        var document = await ReadDocument();

        var content = document["paths"]![path]![method]!["responses"]![status]!["content"]!;

        content["application/problem+json"]!["schema"]!["$ref"]!.GetValue<string>()
            .Should().Be("#/components/schemas/ProblemDetails");
    }

    [Fact]
    public async Task ProblemDetailsSchemaDeclaresCampaignExtensionsAsOptional()
    {
        var document = await ReadDocument();

        var schema = document["components"]!["schemas"]!["ProblemDetails"]!;

        schema["properties"]!["code"]!["type"]!.GetValue<string>().Should().Be("string");
        schema["properties"]!["errors"]!["additionalProperties"]!["items"]!["type"]!.GetValue<string>().Should().Be("string");
        schema["required"].Should().BeNull("framework validation failures carry neither extension");
    }

    private static async Task<JsonNode> ReadDocument()
    {
        using var response = await GetInEnvironment("Development", DocumentPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    private static async Task<HttpResponseMessage> GetInEnvironment(string environment, string path)
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using var client = factory.CreateClient();

        return await client.GetAsync(path);
    }
}
