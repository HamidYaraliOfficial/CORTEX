namespace Cortex.Core.Models;

/// <summary>
/// A single addressable element of the Code Knowledge Graph — a file, a type, a member,
/// a package, an API endpoint, a commit, whatever NodeType describes. Nodes are immutable
/// value snapshots produced by the indexer; identity is <see cref="Id"/>, a stable hash of
/// (RepositoryId, Kind, FullyQualifiedName) so incremental re-indexing can diff safely.
/// </summary>
public sealed class GraphNode
{
    public required string Id { get; init; }
    public required string RepositoryId { get; init; }
    public required NodeType Kind { get; init; }
    public required string DisplayName { get; init; }
    public required string FullyQualifiedName { get; init; }
    public SourceLocation? Location { get; init; }
    public string? ParentId { get; init; }
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();
    public DateTimeOffset IndexedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string ContentHash { get; init; } = string.Empty;

    public static string ComputeId(string repositoryId, NodeType kind, string fullyQualifiedName)
    {
        var raw = $"{repositoryId}::{kind}::{fullyQualifiedName}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
    }
}
