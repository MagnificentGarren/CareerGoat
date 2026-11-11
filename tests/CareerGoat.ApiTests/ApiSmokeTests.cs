using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CareerGoat.Api;

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

    [Theory]
    [InlineData("GET", "/api/v1/profile")]
    [InlineData("PUT", "/api/v1/profile")]
    [InlineData("GET", "/api/v1/target-roles")]
    [InlineData("GET", "/api/v1/target-roles/22222222-2222-2222-2222-222222222222")]
    [InlineData("POST", "/api/v1/target-roles")]
    [InlineData("PUT", "/api/v1/target-roles/22222222-2222-2222-2222-222222222222")]
    [InlineData("DELETE", "/api/v1/target-roles/22222222-2222-2222-2222-222222222222")]
    public async Task UserDataEndpoints_RejectUnauthenticatedRequests(string method, string path)
    {
        using var factory = CreateUserApiFactory(new TestUserDataStore());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "PUT" or "POST")
        {
            request.Content = JsonContent.Create(new
            {
                displayName = "Guest",
                title = "Engineer",
                company = "Example",
                description = "Role description",
            });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotReadUpdateOrDeleteAnotherUsersTargetRole()
    {
        var store = new TestUserDataStore();
        var role = store.SeedRole(UserB, "User B role");
        using var factory = CreateUserApiFactory(store);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "user-a");

        var list = await client.GetFromJsonAsync<List<TargetRoleDto>>("/api/v1/target-roles");
        var detail = await client.GetAsync($"/api/v1/target-roles/{role.Id}");
        var update = await client.PutAsJsonAsync($"/api/v1/target-roles/{role.Id}", new
        {
            title = "Changed by A",
            company = "Attacker",
            description = "Changed description",
        });
        var delete = await client.DeleteAsync($"/api/v1/target-roles/{role.Id}");

        Assert.Empty(list!);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("User B role", store.GetSeededRole(role.Id)?.Title);
    }

    [Fact]
    public async Task UserCanCreateListReadUpdateAndDeleteOwnTargetRole()
    {
        var store = new TestUserDataStore();
        using var factory = CreateUserApiFactory(store);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "user-a");
        var requestBody = new
        {
            title = "Software Engineer",
            company = "Example Co",
            description = "Build secure software.",
        };

        var createdResponse = await client.PostAsJsonAsync("/api/v1/target-roles", requestBody);
        var created = await createdResponse.Content.ReadFromJsonAsync<TargetRoleDto>();
        var listed = await client.GetFromJsonAsync<List<TargetRoleDto>>("/api/v1/target-roles");
        var detail = await client.GetFromJsonAsync<TargetRoleDto>($"/api/v1/target-roles/{created!.Id}");
        var updatedResponse = await client.PutAsJsonAsync(
            $"/api/v1/target-roles/{created.Id}",
            new { title = "Senior Software Engineer", company = "Example Co", description = "Lead a team." });
        var updated = await updatedResponse.Content.ReadFromJsonAsync<TargetRoleDto>();
        var deleted = await client.DeleteAsync($"/api/v1/target-roles/{created.Id}");
        var afterDelete = await client.GetFromJsonAsync<List<TargetRoleDto>>("/api/v1/target-roles");

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal("Software Engineer", created?.Title);
        Assert.Equal(created?.Id, Assert.Single(listed!).Id);
        Assert.Equal(created?.Id, detail?.Id);
        Assert.Equal("Senior Software Engineer", updated?.Title);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(afterDelete!);
    }

    [Fact]
    public async Task ProfileWritesUseAuthenticatedUserRatherThanSuppliedIdentifiers()
    {
        var store = new TestUserDataStore();
        store.SeedProfile(UserA);
        store.SeedProfile(UserB);
        using var factory = CreateUserApiFactory(store);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "user-a");

        var response = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            id = UserB,
            email = "other@example.com",
            displayName = "User A",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedProfile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.Equal(UserA, updatedProfile?.Id);
        Assert.Equal("user-a@example.com", updatedProfile?.Email);
        Assert.Equal("User A", updatedProfile?.DisplayName);
        Assert.Equal("user-b@example.com", store.GetProfile(UserB)?.Email);
        Assert.Equal("user-a@example.com", store.GetProfile(UserA)?.Email);
    }

    private static WebApplicationFactory<Program> CreateUserApiFactory(TestUserDataStore store) =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ISupabaseTokenValidator>();
                    services.AddSingleton<ISupabaseTokenValidator, TestTokenValidator>();
                    services.RemoveAll<IUserDataStore>();
                    services.AddSingleton<IUserDataStore>(store);
                });
            });

    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private sealed class TestTokenValidator : ISupabaseTokenValidator
    {
        public Task<SupabaseUser?> ValidateAsync(string accessToken, CancellationToken cancellationToken) =>
            Task.FromResult<SupabaseUser?>(accessToken switch
            {
                "user-a" => new SupabaseUser(UserA, "user-a@example.com"),
                "user-b" => new SupabaseUser(UserB, "user-b@example.com"),
                _ => null,
            });
    }

    private sealed class TestUserDataStore : IUserDataStore
    {
        private readonly Dictionary<Guid, ProfileDto> _profiles = [];
        private readonly Dictionary<Guid, (Guid UserId, TargetRoleDto Role)> _roles = [];

        public ProfileDto? GetProfile(Guid userId) => _profiles.GetValueOrDefault(userId);

        public TargetRoleDto? GetSeededRole(Guid id) =>
            _roles.TryGetValue(id, out var item) ? item.Role : null;

        public TargetRoleDto SeedRole(Guid userId, string title)
        {
            var now = DateTimeOffset.UtcNow;
            var role = new TargetRoleDto(Guid.NewGuid(), title, null, "Private description", now, now);
            _roles[role.Id] = (userId, role);
            return role;
        }

        public void SeedProfile(Guid userId) =>
            _profiles[userId] = new ProfileDto(
                userId,
                userId == UserB ? "user-b@example.com" : "user-a@example.com",
                null,
                DateTimeOffset.UtcNow);

        public Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_profiles.GetValueOrDefault(userId));

        public Task<ProfileDto?> UpdateProfileAsync(
            Guid userId,
            string? displayName,
            CancellationToken cancellationToken)
        {
            if (!_profiles.TryGetValue(userId, out var profile)) return Task.FromResult<ProfileDto?>(null);
            var updated = profile with { DisplayName = displayName };
            _profiles[userId] = updated;
            return Task.FromResult<ProfileDto?>(updated);
        }

        public Task<IReadOnlyList<TargetRoleDto>> ListTargetRolesAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TargetRoleDto>>(
                _roles.Values.Where(item => item.UserId == userId).Select(item => item.Role).ToList());

        public Task<TargetRoleDto?> GetTargetRoleAsync(
            Guid userId,
            Guid roleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_roles.TryGetValue(roleId, out var item) && item.UserId == userId
                ? item.Role
                : null);

        public Task<TargetRoleDto> CreateTargetRoleAsync(
            Guid userId,
            string title,
            string? company,
            string description,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var role = new TargetRoleDto(Guid.NewGuid(), title, company, description, now, now);
            _roles[role.Id] = (userId, role);
            return Task.FromResult(role);
        }

        public Task<TargetRoleDto?> UpdateTargetRoleAsync(
            Guid userId,
            Guid roleId,
            string title,
            string? company,
            string description,
            CancellationToken cancellationToken)
        {
            if (!_roles.TryGetValue(roleId, out var item) || item.UserId != userId)
                return Task.FromResult<TargetRoleDto?>(null);
            var updated = item.Role with { Title = title, Company = company, Description = description };
            _roles[roleId] = (userId, updated);
            return Task.FromResult<TargetRoleDto?>(updated);
        }

        public Task<bool> DeleteTargetRoleAsync(
            Guid userId,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            if (!_roles.TryGetValue(roleId, out var item) || item.UserId != userId)
                return Task.FromResult(false);
            return Task.FromResult(_roles.Remove(roleId));
        }
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
