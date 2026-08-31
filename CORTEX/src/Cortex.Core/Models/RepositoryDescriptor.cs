namespace Cortex.Core.Models;

public enum RepositorySourceKind { LocalPath, GitHub, GitLab, Bitbucket, RemoteGit }

/// <summary>
/// One repository registered inside a <see cref="WorkspaceDescriptor"/>. Purely metadata —
/// CORTEX never mutates the repository itself outside the explicit, user-confirmed Safe Apply flow.
/// </summary>
public sealed class RepositoryDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required RepositorySourceKind SourceKind { get; init; }
    public required string LocalClonePath { get; init; }
    public string? RemoteUrl { get; init; }
    public string? DefaultBranch { get; init; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset AddedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastIndexedAtUtc { get; set; }
    public string? LastIndexedRevisionSha { get; set; }
}

/// <summary>
/// A named collection of repositories analyzed together, optionally with cross-repository
/// relations declared (e.g. "frontend" calls "backend" over a documented API contract).
/// </summary>
public sealed class WorkspaceDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public List<RepositoryDescriptor> Repositories { get; init; } = new();
    public List<CrossRepositoryRelation> CrossRepositoryRelations { get; init; } = new();
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CrossRepositoryRelation(string SourceRepositoryId, string TargetRepositoryId, string ContractDescription);
