using Cortex.Core.Models;
using Cortex.Graph;

namespace Cortex.Tests;

/// <summary>Small helper building hand-crafted graphs so tests exercise the algorithms, not the Roslyn walker.</summary>
internal static class TestGraphFactory
{
    public const string Repo = "test-repo";

    public static GraphNode Node(NodeType kind, string fqn) => new()
    {
        Id = GraphNode.ComputeId(Repo, kind, fqn),
        RepositoryId = Repo,
        Kind = kind,
        DisplayName = fqn.Split('.').Last(),
        FullyQualifiedName = fqn
    };

    public static GraphEdge Edge(GraphNode from, EdgeType kind, GraphNode to) => new()
    {
        Id = GraphEdge.ComputeId(from.Id, kind, to.Id),
        RepositoryId = Repo,
        SourceNodeId = from.Id,
        TargetNodeId = to.Id,
        Kind = kind
    };

    public static CodeGraph BuildLinearChain(int length, EdgeType edgeKind)
    {
        var graph = new CodeGraph();
        var nodes = Enumerable.Range(0, length).Select(i => Node(NodeType.Class, $"App.Class{i}")).ToList();
        foreach (var n in nodes) graph.AddNode(n);
        for (var i = 0; i < nodes.Count - 1; i++) graph.AddEdge(Edge(nodes[i], edgeKind, nodes[i + 1]));
        return graph;
    }
}
