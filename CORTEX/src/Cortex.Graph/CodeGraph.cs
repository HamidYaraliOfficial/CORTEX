using System.Collections.Concurrent;
using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Graph;

/// <summary>
/// In-memory, thread-safe adjacency-list implementation of the Code Knowledge Graph.
/// Backed on disk by Cortex.Storage (SQLite) for persistence between sessions; this
/// class is the fast working copy the Architecture Canvas, Impact Analyzer and
/// Rule Engine all query directly.
/// </summary>
public sealed class CodeGraph : ICodeGraphEngine
{
    private readonly ConcurrentDictionary<string, GraphNode> _nodes = new();
    private readonly ConcurrentDictionary<string, List<GraphEdge>> _outgoing = new();
    private readonly ConcurrentDictionary<string, List<GraphEdge>> _incoming = new();

    public void AddNode(GraphNode node) => _nodes[node.Id] = node;

    public void AddEdge(GraphEdge edge)
    {
        _outgoing.AddOrUpdate(edge.SourceNodeId,
            _ => new List<GraphEdge> { edge },
            (_, list) => { lock (list) { list.Add(edge); } return list; });

        _incoming.AddOrUpdate(edge.TargetNodeId,
            _ => new List<GraphEdge> { edge },
            (_, list) => { lock (list) { list.Add(edge); } return list; });
    }

    public void RemoveNodesFromFile(string repositoryId, string relativeFilePath)
    {
        var toRemove = _nodes.Values
            .Where(n => n.RepositoryId == repositoryId &&
                        n.Location is not null &&
                        n.Location.RelativeFilePath.Equals(relativeFilePath, StringComparison.OrdinalIgnoreCase))
            .Select(n => n.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _nodes.TryRemove(id, out _);
            _outgoing.TryRemove(id, out _);
            _incoming.TryRemove(id, out _);
        }

        // Drop dangling edges that referenced removed nodes from the other side's adjacency lists.
        foreach (var list in _outgoing.Values) lock (list) list.RemoveAll(e => toRemove.Contains(e.TargetNodeId));
        foreach (var list in _incoming.Values) lock (list) list.RemoveAll(e => toRemove.Contains(e.SourceNodeId));
    }

    public GraphNode? GetNode(string nodeId) => _nodes.GetValueOrDefault(nodeId);

    public IEnumerable<GraphNode> GetAllNodes(string repositoryId) =>
        _nodes.Values.Where(n => n.RepositoryId == repositoryId);

    public IEnumerable<GraphEdge> GetOutgoingEdges(string nodeId, EdgeType? filter = null) =>
        _outgoing.TryGetValue(nodeId, out var list)
            ? (filter is null ? list : list.Where(e => e.Kind == filter)).ToList()
            : Enumerable.Empty<GraphEdge>();

    public IEnumerable<GraphEdge> GetIncomingEdges(string nodeId, EdgeType? filter = null) =>
        _incoming.TryGetValue(nodeId, out var list)
            ? (filter is null ? list : list.Where(e => e.Kind == filter)).ToList()
            : Enumerable.Empty<GraphEdge>();

    public IReadOnlyList<GraphNode> FindByName(string repositoryId, string nameFragment, int maxResults = 50) =>
        _nodes.Values
            .Where(n => n.RepositoryId == repositoryId &&
                        n.DisplayName.Contains(nameFragment, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

    public int NodeCount(string repositoryId) => _nodes.Values.Count(n => n.RepositoryId == repositoryId);
    public int EdgeCount() => _outgoing.Values.Sum(l => l.Count);
}
