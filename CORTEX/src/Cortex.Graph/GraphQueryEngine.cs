using System.Text.RegularExpressions;
using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Graph;

/// <summary>
/// Very small "Question-to-Graph Query" front end: recognizes a handful of common
/// natural-language question shapes ("who calls X", "what implements X", "what does
/// X depend on") and turns them into deterministic graph traversals. This is the
/// non-AI fallback used when Cloud AI is disabled, and the first tool the
/// AI Codebase Assistant reaches for before falling back to embedding search.
/// </summary>
public sealed class GraphQueryEngine
{
    private readonly ICodeGraphEngine _graph;
    private readonly GraphTraversal _traversal;

    public GraphQueryEngine(ICodeGraphEngine graph)
    {
        _graph = graph;
        _traversal = new GraphTraversal(graph);
    }

    private static readonly (Regex Pattern, EdgeType Edge, bool Inward)[] Patterns =
    {
        (new Regex(@"who\s+calls\s+(.+)", RegexOptions.IgnoreCase), EdgeType.Calls, true),
        (new Regex(@"what\s+does\s+(.+)\s+call", RegexOptions.IgnoreCase), EdgeType.Calls, false),
        (new Regex(@"what\s+implements\s+(.+)", RegexOptions.IgnoreCase), EdgeType.Implements, true),
        (new Regex(@"what\s+inherits\s+from\s+(.+)", RegexOptions.IgnoreCase), EdgeType.Inherits, true),
        (new Regex(@"what\s+does\s+(.+)\s+depend\s+on", RegexOptions.IgnoreCase), EdgeType.DependsOn, false),
        (new Regex(@"who\s+depends\s+on\s+(.+)", RegexOptions.IgnoreCase), EdgeType.DependsOn, true),
        (new Regex(@"what\s+references\s+(.+)", RegexOptions.IgnoreCase), EdgeType.References, true),
        (new Regex(@"what\s+tests\s+cover\s+(.+)", RegexOptions.IgnoreCase), EdgeType.Tests, true),
    };

    public sealed record QueryResult(bool Matched, string? SubjectName, EdgeType? Edge, IReadOnlyList<GraphNode> Nodes);

    public QueryResult Ask(string repositoryId, string naturalLanguageQuestion)
    {
        foreach (var (pattern, edge, inward) in Patterns)
        {
            var match = pattern.Match(naturalLanguageQuestion.Trim());
            if (!match.Success) continue;

            var subject = match.Groups[1].Value.Trim().TrimEnd('?', '.');
            var candidates = _graph.FindByName(repositoryId, subject, 5);
            if (candidates.Count == 0) return new QueryResult(true, subject, edge, Array.Empty<GraphNode>());

            var root = candidates[0];
            var hop = inward
                ? _traversal.BreadthFirstInward(root.Id, 1, new HashSet<EdgeType> { edge })
                : _traversal.BreadthFirstOutward(root.Id, 1, new HashSet<EdgeType> { edge });

            var nodes = hop.Where(t => t.Depth == 1).Select(t => t.Node).ToList();
            return new QueryResult(true, subject, edge, nodes);
        }

        return new QueryResult(false, null, null, Array.Empty<GraphNode>());
    }
}
