using Cortex.Core.Abstractions;

namespace Cortex.Reports;

/// <summary>
/// Assembles a full Architecture Review Report from the outputs of every analysis
/// subsystem. Every recommendation line carries a source and, where it originated from
/// the AI Architecture Reviewer, a confidence value — nothing is asserted without a trail.
/// </summary>
public sealed class ReportGenerator
{
    public sealed record Recommendation(string Text, string Source, double? Confidence);

    public sealed record ArchitectureReviewReport(
        string RepositoryId, string RevisionSha, DateTimeOffset GeneratedAtUtc,
        string ExecutiveSummary,
        ArchitectureHealth Overview,
        IReadOnlyList<RuleViolation> DependencyIssues,
        IReadOnlyList<(IReadOnlyList<string> CycleNodeNames, int EdgeCount)> CircularDependencies,
        IReadOnlyList<ModuleMetrics> ComplexityHotspots,
        IReadOnlyList<string> ApiRiskNotes,
        IReadOnlyList<string> TestWeaknessNotes,
        IReadOnlyList<ImpactReport> RecentChangeImpacts,
        IReadOnlyList<Recommendation> Recommendations);

    public ArchitectureReviewReport Build(
        string repositoryId, string revisionSha,
        ArchitectureHealth health,
        IReadOnlyList<RuleViolation> ruleViolations,
        IReadOnlyList<(IReadOnlyList<string>, int)> cycles,
        IReadOnlyList<ModuleMetrics> allMetrics,
        IReadOnlyList<ImpactReport> recentImpacts,
        IReadOnlyList<Recommendation> recommendations)
    {
        var hotspots = allMetrics
            .OrderByDescending(m => m.CyclomaticComplexity + m.FanOut)
            .Take(15)
            .ToList();

        var apiRisk = recentImpacts
            .Where(r => r.AffectedApiContracts.Count > 0)
            .Select(r => $"'{r.RootNodeId}' change touches {r.AffectedApiContracts.Count} public API contract(s) — Impact: {r.Level}")
            .ToList();

        var testWeakness = recentImpacts
            .Where(r => r.AffectedTests.Count == 0)
            .Select(r => $"'{r.RootNodeId}' has no covering tests despite Impact level {r.Level}")
            .ToList();

        var summary =
            $"Architecture Health Score: {health.Score0To100}/100. " +
            $"{ruleViolations.Count} rule violation(s), {cycles.Count} circular dependency cycle(s), " +
            $"{hotspots.Count(m => m.CyclomaticComplexity > 10)} high-complexity hotspot(s). " +
            $"This summary is an analytical snapshot at revision {revisionSha[..Math.Min(7, revisionSha.Length)]}, not a certification of code quality.";

        return new ArchitectureReviewReport(
            repositoryId, revisionSha, DateTimeOffset.UtcNow, summary, health,
            ruleViolations, cycles, hotspots, apiRisk, testWeakness, recentImpacts, recommendations);
    }
}
