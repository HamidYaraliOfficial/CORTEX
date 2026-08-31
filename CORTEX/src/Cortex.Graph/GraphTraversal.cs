using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Graph;

/// <summary>
/// BFS/DFS/neighborhood/path-finding primitives shared by Focus Mode, Impact Analysis,
/// the Path Finder tool and the AI's graph-traversal tool.
/// </summary>
public sealed class GraphTraversal
{
    private readonly ICodeGraphEngine _graph;
    public GraphTraversal(ICodeGraphEngine graph) => _graph = graph;

    /// <summary>Breadth-first walk outward from <paramref name="startNodeId"/>, following outgoing edges.</summary>
    public IReadOnlyList<(GraphNode Node, int Depth, EdgeType? Via)> BreadthFirstOutward(
        string startNodeId, int maxDepth, IReadOnlySet<EdgeType>? edgeFilter = null)
    {
        var results = new List<(GraphNode, int, EdgeType?)>();
        var visited = new HashSet<string> { startNodeId };
        var queue = new Queue<(string NodeId, int Depth, EdgeType? Via)>();
        queue.Enqueue((startNodeId, 0, null));

        while (queue.Count > 0)
        {
            var (nodeId, depth, via) = queue.Dequeue();
            var node = _graph.GetNode(nodeId);
            if (node is not null) results.Add((node, depth, via));
            if (depth >= maxDepth) continue;

            foreach (var edge in _graph.GetOutgoingEdges(nodeId))
            {
                if (edgeFilter is not null && !edgeFilter.Contains(edge.Kind)) continue;
                if (!visited.Add(edge.TargetNodeId)) continue;
                queue.Enqueue((edge.TargetNodeId, depth + 1, edge.Kind));
            }
        }
        return results;
    }

    /// <summary>Same as <see cref="BreadthFirstOutward"/> but following incoming edges (who depends on me?).</summary>
    public IReadOnlyList<(GraphNode Node, int Depth, EdgeType? Via)> BreadthFirstInward(
        string startNodeId, int maxDepth, IReadOnlySet<EdgeType>? edgeFilter = null)
    {
        var results = new List<(GraphNode, int, EdgeType?)>();
        var visited = new HashSet<string> { startNodeId };
        var queue = new Queue<(string NodeId, int Depth, EdgeType? Via)>();
        queue.Enqueue((startNodeId, 0, null));

        while (queue.Count > 0)
        {
            var (nodeId, depth, via) = queue.Dequeue();
            var node = _graph.GetNode(nodeId);
            if (node is not null) results.Add((node, depth, via));
            if (depth >= maxDepth) continue;

            foreach (var edge in _graph.GetIncomingEdges(nodeId))
            {
                if (edgeFilter is not null && !edgeFilter.Contains(edge.Kind)) continue;
                if (!visited.Add(edge.SourceNodeId)) continue;
                queue.Enqueue((edge.SourceNodeId, depth + 1, edge.Kind));
            }
        }
        return results;
    }

    /// <summary>Powers the Path Finder panel: every simple path up to <paramref name="maxDepth"/> between two symbols.</summary>
    public IReadOnlyList<IReadOnlyList<GraphEdge>> FindAllPaths(string fromNodeId, string toNodeId, int maxDepth = 8)
    {
        var results = new List<IReadOnlyList<GraphEdge>>();
        var path = new List<GraphEdge>();
        var onPath = new HashSet<string> { fromNodeId };

        void Dfs(string current, int depth)
        {
            if (depth > maxDepth) return;
            if (current == toNodeId && path.Count > 0)
            {
                results.Add(path.ToList());
                return;
            }
            foreach (var edge in _graph.GetOutgoingEdges(current))
            {
                if (onPath.Contains(edge.TargetNodeId)) continue; // no cycles inside a single path
                path.Add(edge);
                onPath.Add(edge.TargetNodeId);
                Dfs(edge.TargetNodeId, depth + 1);
                onPath.Remove(edge.TargetNodeId);
                path.RemoveAt(path.Count - 1);
            }
        }

        Dfs(fromNodeId, 0);
        return results;
    }

    /// <summary>Powers Focus Mode: the local neighborhood of a node up to N hops, both directions.</summary>
    public (IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges) Neighborhood(string nodeId, int hops = 1)
    {
        var outward = BreadthFirstOutward(nodeId, hops).Select(t => t.Node.Id).ToHashSet();
        var inward = BreadthFirstInward(nodeId, hops).Select(t => t.Node.Id).ToHashSet();
        var union = outward.Union(inward).ToHashSet();

        var nodes = union.Select(_graph.GetNode).Where(n => n is not null).Select(n => n!).ToList();
        var edges = union
            .SelectMany(id => _graph.GetOutgoingEdges(id))
            .Where(e => union.Contains(e.SourceNodeId) && union.Contains(e.TargetNodeId))
            .DistinctBy(e => e.Id)
            .ToList();

        return (nodes, edges);
    }
}
