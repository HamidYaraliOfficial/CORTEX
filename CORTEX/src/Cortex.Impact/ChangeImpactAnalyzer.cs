using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Git;
using Cortex.Graph;

namespace Cortex.Impact;

/// <summary>
/// The heart of CORTEX. Given one node (a file, class, method, interface, ...), traces
/// every kind of relationship that matters for "what breaks if I change this": direct
/// dependents (callers/consumers), indirect (transitive) dependents, implementations of
/// a changed interface, affected tests, and affected API contracts — then folds all of
/// that into a single, explainable Low/Medium/High/Critical level.
/// </summary>
public sealed class ChangeImpactAnalyzer : IImpactAnalyzer
{
    private readonly ICodeGraphEngine _graph;
    private readonly GraphTraversal _traversal;
    private readonly CommitImpactAnalyzer? _commitAnalyzer;

    private static readonly HashSet<EdgeType> InboundImpactEdges = new()
    {
        EdgeType.Calls, EdgeType.References, EdgeType.Uses, EdgeType.DependsOn,
        EdgeType.Implements, EdgeType.Inherits, EdgeType.Instantiates, EdgeType.Overrides
    };

    public ChangeImpactAnalyzer(ICodeGraphEngine graph, CommitImpactAnalyzer? commitAnalyzer = null)
    {
        _graph = graph;
        _traversal = new GraphTraversal(graph);
        _commitAnalyzer = commitAnalyzer;
    }

    public ImpactReport AnalyzeNodeImpact(string repositoryId, string nodeId, ImpactOptions options)
    {
        var root = _graph.GetNode(nodeId) ?? throw new ArgumentException($"Unknown node {nodeId}");

        var directHop = _traversal.BreadthFirstInward(nodeId, 1, InboundImpactEdges)
            .Where(t => t.Depth == 1)
            .Select(t => new ImpactedItem(t.Node.Id, t.Node.DisplayName, t.Node.Kind, t.Via ?? EdgeType.References, 1))
            .ToList();

        var allHops = options.IncludeIndirect
            ? _traversal.BreadthFirstInward(nodeId, options.MaxDepth, InboundImpactEdges)
            : Array.Empty<(GraphNode, int, EdgeType?)>();

        var indirect = allHops
            .Where(t => t.Depth > 1)
            .Select(t => new ImpactedItem(t.Node.Id, t.Node.DisplayName, t.Node.Kind, t.Via ?? EdgeType.References, t.Depth))
            .ToList();

        var tests = options.IncludeTests
            ? _traversal.BreadthFirstInward(nodeId, options.MaxDepth, new HashSet<EdgeType> { EdgeType.Tests }.Union(InboundImpactEdges).ToHashSet())
                .Where(t => t.Node.Kind is NodeType.TestClass or NodeType.TestMethod)
                .Select(t => new ImpactedItem(t.Node.Id, t.Node.DisplayName, t.Node.Kind, EdgeType.Tests, t.Depth))
                .ToList()
            : new List<ImpactedItem>();

        var apiContracts = allHops
            .Where(t => t.Node.Kind is NodeType.ApiEndpoint or NodeType.Controller or NodeType.Dto)
            .Select(t => new ImpactedItem(t.Node.Id, t.Node.DisplayName, t.Node.Kind, t.Via ?? EdgeType.Exposes, t.Depth))
            .ToList();

        var level = ScoreImpact(root, directHop.Count, indirect.Count, apiContracts.Count, tests.Count);

        var summary = $"Changing '{root.DisplayName}' directly affects {directHop.Count} item(s) " +
                      $"and reaches {indirect.Count} more transitively" +
                      (apiContracts.Count > 0 ? $", including {apiContracts.Count} public API contract(s)" : "") +
                      (tests.Count > 0 ? $"; {tests.Count} test(s) cover this area." : "; no tests currently cover this area.");

        return new ImpactReport(nodeId, level, directHop, indirect, tests, apiContracts, summary);
    }

    public ImpactReport AnalyzeCommitImpact(string repositoryId, string commitSha)
    {
        if (_commitAnalyzer is null)
            throw new InvalidOperationException("Commit impact requires a CommitImpactAnalyzer (Git integration) to be configured.");

        // The Commit Timeline UI resolves the commit's touched nodes first, then unions
        // per-node impact reports — kept here as the documented composition contract.
        throw new NotSupportedException(
            "Call CommitImpactAnalyzer.Analyze(...) to resolve touched nodes, then AnalyzeNodeImpact(...) per node and union the results.");
    }

    private static ImpactLevel ScoreImpact(GraphNode root, int direct, int indirect, int apiContracts, int tests)
    {
        var isPublicSurface = root.Kind is NodeType.Interface or NodeType.ApiEndpoint or NodeType.Controller or NodeType.Dto;
        var score = direct * 2 + indirect + apiContracts * 5 + (isPublicSurface ? 10 : 0) - Math.Min(tests, 10);

        return score switch
        {
            <= 3 => ImpactLevel.Low,
            <= 12 => ImpactLevel.Medium,
            <= 30 => ImpactLevel.High,
            _ => ImpactLevel.Critical
        };
    }
}
