using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cortex.Metrics;

/// <summary>
/// Computes per-file structural metrics (LOC, cyclomatic complexity, class/method counts)
/// from source text, and Fan-In/Fan-Out/coupling from the Code Knowledge Graph. Every
/// threshold used to flag a file as "too complex" is configurable — see <see cref="Thresholds"/>.
/// </summary>
public sealed class ComplexityMetricsCalculator : IMetricsEngine
{
    private readonly ICodeGraphEngine _graph;
    public ComplexityMetricsCalculator(ICodeGraphEngine graph) => _graph = graph;

    public sealed record MetricThresholds(int MaxCyclomaticComplexity = 10, int MaxMethodLoc = 60, int MaxFanOut = 15, int MaxClassMethodCount = 25);
    public MetricThresholds Thresholds { get; set; } = new();

    public ModuleMetrics ComputeForFile(string repositoryId, string relativeFilePath, string sourceText)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = tree.GetRoot();

        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var loc = sourceText.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"));
        var complexity = methods.Sum(SyntaxComplexityWalker.Compute);

        var fileNodeId = GraphNode.ComputeId(repositoryId, NodeType.File, relativeFilePath);
        var fanOut = _graph.GetOutgoingEdges(fileNodeId, EdgeType.DependsOn).Select(e => e.TargetNodeId).Distinct().Count();
        var fanIn = _graph.GetIncomingEdges(fileNodeId, EdgeType.DependsOn).Select(e => e.SourceNodeId).Distinct().Count();
        var couplingProxy = fanIn + fanOut == 0 ? 0 : Math.Round((double)fanOut / (fanIn + fanOut), 2);

        return new ModuleMetrics(relativeFilePath, loc, complexity, methods.Count, classes.Count, fanIn, fanOut, couplingProxy);
    }

    public ArchitectureHealth ComputeHealthScore(
        string repositoryId, IReadOnlyList<ModuleMetrics> allMetrics, IReadOnlyList<RuleViolation> violations, int circularDependencyCount)
    {
        var score = 100.0;
        var factors = new List<string>();

        var highComplexity = allMetrics.Count(m => m.CyclomaticComplexity > Thresholds.MaxCyclomaticComplexity);
        if (highComplexity > 0) { score -= Math.Min(20, highComplexity * 1.5); factors.Add($"{highComplexity} high-complexity file(s)"); }

        var highFanOut = allMetrics.Count(m => m.FanOut > Thresholds.MaxFanOut);
        if (highFanOut > 0) { score -= Math.Min(15, highFanOut * 1.2); factors.Add($"{highFanOut} high-coupling file(s)"); }

        if (circularDependencyCount > 0) { score -= Math.Min(25, circularDependencyCount * 5); factors.Add($"{circularDependencyCount} circular dependency cycle(s)"); }

        var errorViolations = violations.Count(v => v.Rule.Severity == RuleSeverity.Error);
        if (errorViolations > 0) { score -= Math.Min(20, errorViolations * 4); factors.Add($"{errorViolations} architecture rule violation(s)"); }

        return new ArchitectureHealth(Math.Max(0, (int)Math.Round(score)), factors);
    }
}
