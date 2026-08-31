using System.Text;
using System.Text.Json;
using Cortex.Core.Models;

namespace Cortex.Reports;

/// <summary>
/// Exports the Architecture Graph, Dependency Map, Impact Reports and Metrics to
/// JSON, CSV, Markdown or HTML. Every export is stamped with the repository revision,
/// a UTC timestamp and a data-source note, per CORTEX's export-provenance requirement.
/// PDF export is intentionally left as an extension point — wire a dedicated PDF
/// rendering library (e.g. QuestPDF) at the call site rather than hand-rolling PDF here.
/// </summary>
public sealed class ExportService
{
    public async Task ExportGraphAsJsonAsync(string outputPath, string repositoryId, string revisionSha,
        IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges, CancellationToken ct)
    {
        var envelope = new
        {
            repositoryId,
            revisionSha,
            exportedAtUtc = DateTimeOffset.UtcNow,
            dataSource = "CORTEX Code Knowledge Graph",
            nodes,
            edges
        };
        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, envelope, new JsonSerializerOptions { WriteIndented = true }, ct);
    }

    public async Task ExportMetricsAsCsvAsync(string outputPath, IReadOnlyList<ModuleMetrics> metrics, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("RelativeFilePath,LinesOfCode,CyclomaticComplexity,MethodCount,ClassCount,FanIn,FanOut,CouplingProxy");
        foreach (var m in metrics)
        {
            sb.AppendLine($"\"{m.RelativeFilePath}\",{m.LinesOfCode},{m.CyclomaticComplexity},{m.MethodCount},{m.ClassCount},{m.FanIn},{m.FanOut},{m.CouplingProxy}");
        }
        await File.WriteAllTextAsync(outputPath, sb.ToString(), ct);
    }

    public async Task ExportReportAsMarkdownAsync(string outputPath, ReportGenerator.ArchitectureReviewReport report, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Architecture Review — {report.RepositoryId}");
        sb.AppendLine($"_Revision `{report.RevisionSha}` · Generated {report.GeneratedAtUtc:u}_\n");
        sb.AppendLine("## Executive Summary\n" + report.ExecutiveSummary + "\n");
        sb.AppendLine($"## Architecture Health\nScore: **{report.Overview.Score0To100}/100**\n");
        foreach (var f in report.Overview.ContributingFactors) sb.AppendLine($"- {f}");
        sb.AppendLine($"\n> {report.Overview.Disclaimer}\n");

        sb.AppendLine("## Dependency Issues");
        foreach (var v in report.DependencyIssues) sb.AppendLine($"- **{v.Rule.Severity}** `{v.Rule.Name}`: {v.Explanation}");

        sb.AppendLine("\n## Circular Dependencies");
        foreach (var (names, edgeCount) in report.CircularDependencies)
            sb.AppendLine($"- Cycle of {names.Count} node(s), {edgeCount} edge(s): {string.Join(" → ", names)}");

        sb.AppendLine("\n## Complexity Hotspots");
        foreach (var m in report.ComplexityHotspots) sb.AppendLine($"- `{m.RelativeFilePath}` — complexity {m.CyclomaticComplexity}, fan-out {m.FanOut}");

        sb.AppendLine("\n## API Risk");
        foreach (var note in report.ApiRiskNotes) sb.AppendLine($"- {note}");

        sb.AppendLine("\n## Test Weakness");
        foreach (var note in report.TestWeaknessNotes) sb.AppendLine($"- {note}");

        sb.AppendLine("\n## Recommendations");
        foreach (var r in report.Recommendations)
            sb.AppendLine($"- {r.Text} _(source: {r.Source}{(r.Confidence is null ? "" : $", confidence {r.Confidence:P0}")})_");

        await File.WriteAllTextAsync(outputPath, sb.ToString(), ct);
    }
}
