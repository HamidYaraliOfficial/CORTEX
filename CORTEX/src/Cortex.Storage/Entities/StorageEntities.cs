namespace Cortex.Storage.Entities;

public sealed class NodeEntity
{
    public string Id { get; set; } = "";
    public string RepositoryId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FullyQualifiedName { get; set; } = "";
    public string? ParentId { get; set; }
    public string? RelativeFilePath { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string AttributesJson { get; set; } = "{}";
    public DateTimeOffset IndexedAtUtc { get; set; }
}

public sealed class EdgeEntity
{
    public string Id { get; set; } = "";
    public string RepositoryId { get; set; } = "";
    public string SourceNodeId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public string Kind { get; set; } = "";
    public string EvidenceJson { get; set; } = "[]";
    public double Weight { get; set; } = 1.0;
}

public sealed class RepositoryEntity
{
    public string Id { get; set; } = "";
    public string WorkspaceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string SourceKind { get; set; } = "";
    public string LocalClonePath { get; set; } = "";
    public string? RemoteUrl { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset AddedAtUtc { get; set; }
    public DateTimeOffset? LastIndexedAtUtc { get; set; }
    public string? LastIndexedRevisionSha { get; set; }
}

public sealed class WorkspaceEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SnapshotEntity
{
    public string Id { get; set; } = "";
    public string RepositoryId { get; set; } = "";
    public string RevisionSha { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string GraphJsonPath { get; set; } = "";
    public string MetricsJsonPath { get; set; } = "";
    public string? Label { get; set; }
}

public sealed class AuditEventEntity
{
    public long Id { get; set; }
    public DateTimeOffset AtUtc { get; set; }
    public string Category { get; set; } = "";
    public string Action { get; set; } = "";
    public string? RepositoryId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class RuleEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SourcePattern { get; set; } = "";
    public string ForbiddenTargetPattern { get; set; } = "";
    public string Severity { get; set; } = "Warning";
    public bool Enabled { get; set; } = true;
    public int Version { get; set; } = 1;
}

public sealed class FileHashEntity
{
    public string RepositoryId { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public DateTimeOffset LastWriteUtc { get; set; }
}
