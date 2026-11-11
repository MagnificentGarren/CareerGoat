using CareerGoat.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>() ?? [];

if (!builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Configure at least one Frontend:AllowedOrigins entry outside Development.");
}

foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        || uri.GetLeftPart(UriPartial.Authority) != origin
        || (!builder.Environment.IsDevelopment() && uri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            $"Frontend:AllowedOrigins contains an invalid origin: {origin}");
    }
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddSingleton<IAiAnalysisProvider, FakeAiAnalysisProvider>();
builder.Services.AddHttpClient<ISupabaseTokenValidator, SupabaseTokenValidator>(
    client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddAuthentication("Supabase")
    .AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddDbContext<CareerGoatDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>()
        .GetConnectionString("CareerGoat");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Configure ConnectionStrings:CareerGoat with the Supabase PostgreSQL connection string.");
    }

    var postgres = new NpgsqlConnectionStringBuilder(connectionString);
    if (!services.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
        && postgres.SslMode != SslMode.VerifyFull)
    {
        throw new InvalidOperationException(
            "ConnectionStrings:CareerGoat must enable SSL Mode=VerifyFull outside Development.");
    }

    options.UseNpgsql(postgres.ConnectionString);
});
builder.Services.AddScoped<IUserDataStore, EfUserDataStore>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var userApi = app.MapGroup("/api/v1").RequireAuthorization();

userApi.MapGet("/profile", async (
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
    await dataStore.GetProfileAsync(UserId(context), cancellationToken) is { } profile
        ? Results.Ok(profile)
        : Results.NotFound());

userApi.MapPut("/profile", async (
    UpdateProfileRequest request,
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
{
    if (request.DisplayName is { Length: > 120 })
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.DisplayName)] = ["Display name must be 120 characters or fewer."]
        });
    }

    var profile = await dataStore.UpdateProfileAsync(
        UserId(context),
        string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
        cancellationToken);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

userApi.MapGet("/target-roles", async (
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
    Results.Ok(await dataStore.ListTargetRolesAsync(UserId(context), cancellationToken)));

userApi.MapGet("/target-roles/{id:guid}", async (
    Guid id,
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
    await dataStore.GetTargetRoleAsync(UserId(context), id, cancellationToken) is { } role
        ? Results.Ok(role)
        : Results.NotFound());

userApi.MapPost("/target-roles", async (
    TargetRoleRequest request,
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
{
    var errors = Validate(request);
    if (errors.Count != 0)
    {
        return Results.ValidationProblem(errors);
    }

    var role = await dataStore.CreateTargetRoleAsync(
        UserId(context),
        request.Title.Trim(),
        request.Company?.Trim(),
        request.Description.Trim(),
        cancellationToken);
    return Results.Created($"/api/v1/target-roles/{role.Id}", role);
});

userApi.MapPut("/target-roles/{id:guid}", async (
    Guid id,
    TargetRoleRequest request,
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
{
    var errors = Validate(request);
    if (errors.Count != 0)
    {
        return Results.ValidationProblem(errors);
    }

    var role = await dataStore.UpdateTargetRoleAsync(
        UserId(context),
        id,
        request.Title.Trim(),
        request.Company?.Trim(),
        request.Description.Trim(),
        cancellationToken);
    return role is null ? Results.NotFound() : Results.Ok(role);
});

userApi.MapDelete("/target-roles/{id:guid}", async (
    Guid id,
    HttpContext context,
    IUserDataStore dataStore,
    CancellationToken cancellationToken) =>
    await dataStore.DeleteTargetRoleAsync(UserId(context), id, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound());

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/v1/demo-analysis", async (
        IAiAnalysisProvider provider,
        CancellationToken cancellationToken) =>
    {
        var result = await provider.AnalyzeAsync(
            new AnalysisRequest("Software Engineer"),
            cancellationToken);

        return Results.Ok(result);
    });
}

static Guid UserId(HttpContext context) => Guid.Parse(
    context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

static Dictionary<string, string[]> Validate(TargetRoleRequest request)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160)
    {
        errors[nameof(request.Title)] = ["Title is required and must be 160 characters or fewer."];
    }

    if (request.Company?.Length > 160)
    {
        errors[nameof(request.Company)] = ["Company must be 160 characters or fewer."];
    }

    if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 12000)
    {
        errors[nameof(request.Description)] = ["Description is required and must be 12,000 characters or fewer."];
    }

    return errors;
}

app.Run();

public partial class Program { }
