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
    public async Task HealthEndpoint_AllowsConfiguredStagingOrigin()
    {
        using var stagingOrigin = SetEnvironmentVariable(
            "Frontend__AllowedOrigins__0",
            "https://careergoat-preview.vercel.app");
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Staging"));
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://careergoat-preview.vercel.app");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "https://careergoat-preview.vercel.app",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
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
        using var stagingOrigin = SetEnvironmentVariable(
            "Frontend__AllowedOrigins__0",
            "https://careergoat-preview.vercel.app");
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/demo-analysis");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record HealthResponse(string Status);

    private sealed record AnalysisResponse(string RoleTitle, string Summary);

    private static IDisposable SetEnvironmentVariable(string name, string value)
    {
        var previousValue = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return new EnvironmentVariableScope(name, previousValue);
    }

    private sealed class EnvironmentVariableScope(string name, string? previousValue) : IDisposable
    {
        public void Dispose() => Environment.SetEnvironmentVariable(name, previousValue);
    }
}
