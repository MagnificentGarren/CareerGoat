namespace CareerGoat.Api;

public sealed class FakeAiAnalysisProvider : IAiAnalysisProvider
{
    public Task<CareerAnalysis> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new CareerAnalysis(
            request.RoleTitle,
            72,
            "This sample is generated locally by the fake provider. No AI service was contacted.",
            ["Cloud deployment", "Automated testing"]);

        return Task.FromResult(result);
    }
}
