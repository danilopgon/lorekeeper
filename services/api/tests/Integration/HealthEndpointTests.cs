using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration;

public class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        await using var factory = new WebApplicationFactory<Program>().WithoutIngestionWorker();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
