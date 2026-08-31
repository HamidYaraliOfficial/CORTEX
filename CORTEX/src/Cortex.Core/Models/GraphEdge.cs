namespace Cortex.Core.Models;

/// <summary>
/// A single directed, typed relationship between two <see cref="GraphNode"/>s.
/// </summary>
public sealed class GraphEdge
{
    public required string Id { get; init; }
    public required string RepositoryId { get; init; }
    public required string SourceNodeId { get; init; }
    public required string TargetNodeId { get; init; }
    public required EdgeType Kind { get; init; }
    public IReadOnlyList<EdgeEvidence> Evidence { get; init; } = Array.Empty<EdgeEvidence>();
    public double Weight { get; init; } = 1.0;

    public static string ComputeId(string sourceNodeId, EdgeType kind, string targetNodeId, string? disambiguator = null) =>
        GraphNode.ComputeId(sourceNodeId, NodeType.File, $"{kind}::{targetNodeId}::{disambiguator}");
}
