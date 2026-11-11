namespace CareerGoat.Api;

public interface IAiAnalysisProvider
{
    Task<CareerAnalysis> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken);
}

public sealed record AnalysisRequest(string RoleTitle);

public sealed record CareerAnalysis(
    string RoleTitle,
    int MatchScore,
    string Summary,
    IReadOnlyList<string> SkillGaps);
