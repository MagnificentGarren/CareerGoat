using CareerGoat.Api;

namespace CareerGoat.UnitTests;

public class FakeAiAnalysisProviderTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsDeterministicLocalSample()
    {
        var provider = new FakeAiAnalysisProvider();

        var result = await provider.AnalyzeAsync(
            new AnalysisRequest("Software Engineer"),
            CancellationToken.None);

        Assert.Equal("Software Engineer", result.RoleTitle);
        Assert.InRange(result.MatchScore, 0, 100);
        Assert.Contains("locally", result.Summary);
        Assert.NotEmpty(result.SkillGaps);
    }

    [Fact]
    public async Task AnalyzeAsync_ObservesCancellation()
    {
        var provider = new FakeAiAnalysisProvider();
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.AnalyzeAsync(
                new AnalysisRequest("Software Engineer"),
                cancellationSource.Token));
    }
}
