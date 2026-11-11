using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using CareerGoat.Api;

namespace CareerGoat.UnitTests;

public class SupabaseTokenValidatorTests
{
    [Fact]
    public async Task ValidateAsync_UsesSupabaseAuthResponseAsUserIdentity()
    {
        var userId = Guid.NewGuid();
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"id":"{{userId}}","email":"test@example.com"}""",
                Encoding.UTF8,
                "application/json"),
        });
        using var client = new HttpClient(handler);
        var validator = CreateValidator(client, "Production");

        var user = await validator.ValidateAsync("signed-access-token", CancellationToken.None);

        Assert.Equal(userId, user?.Id);
        Assert.Equal("test@example.com", user?.Email);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("https://careergoat-staging.supabase.co/auth/v1/user", handler.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("signed-access-token", handler.AuthorizationParameter);
        Assert.Equal("public-anon-key", handler.ApiKey);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNullWhenSupabaseRejectsTheToken()
    {
        using var client = new HttpClient(
            new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var validator = CreateValidator(client, "Production");

        var user = await validator.ValidateAsync("expired-access-token", CancellationToken.None);

        Assert.Null(user);
    }

    [Fact]
    public async Task ValidateAsync_RejectsInsecureNonLocalSupabaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "http://careergoat-staging.supabase.co",
                ["Supabase:AnonKey"] = "public-anon-key",
            })
            .Build();
        using var client = new HttpClient(new CapturingHandler(
            _ => throw new InvalidOperationException("The request must not be sent.")));
        var validator = new SupabaseTokenValidator(client, configuration, new TestEnvironment("Production"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.ValidateAsync("access-token", CancellationToken.None));
    }

    private static SupabaseTokenValidator CreateValidator(HttpClient client, string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://careergoat-staging.supabase.co",
                ["Supabase:AnonKey"] = "public-anon-key",
            })
            .Build();
        return new SupabaseTokenValidator(client, configuration, new TestEnvironment(environmentName));
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string? ApiKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            ApiKey = request.Headers.GetValues("apikey").Single();
            return Task.FromResult(respond(request));
        }
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "CareerGoat.UnitTests";
        public string ContentRootPath { get; set; } = Environment.CurrentDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
