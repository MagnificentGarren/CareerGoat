using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CareerGoat.ApiTests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", (await response.Content.ReadFromJsonAsync<HealthResponse>())?.Status);
    }

    [Fact]
    public async Task DemoAnalysis_UsesLocalFakeProvider()
    {
        var response = await _client.GetAsync("/api/v1/demo-analysis");
        var result = await response.Content.ReadFromJsonAsync<AnalysisResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Software Engineer", result?.RoleTitle);
        Assert.Contains("No AI service was contacted", result?.Summary);
    }

    [Fact]
    public async Task DemoAnalysis_IsUnavailableOutsideDevelopment()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/demo-analysis");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record HealthResponse(string Status);

    private sealed record AnalysisResponse(string RoleTitle, string Summary);
}
