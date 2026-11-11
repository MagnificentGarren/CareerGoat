using CareerGoat.Api;

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

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

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

app.Run();

public partial class Program { }
