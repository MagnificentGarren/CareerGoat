using CareerGoat.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Frontend:AllowedOrigins")
        .Get<string[]>() ?? [];

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
