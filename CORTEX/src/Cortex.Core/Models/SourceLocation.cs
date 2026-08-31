namespace Cortex.Core.Models;

/// <summary>
/// Precise origin of a fact CORTEX extracted. Attached to every graph node and every
/// edge so the UI can always answer "where did this come from?" instead of asking the
/// user to trust a black box.
/// </summary>
public sealed record SourceLocation(
    string RepositoryId,
    string RelativeFilePath,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    string? RevisionSha = null)
{
    public override string ToString() =>
        $"{RelativeFilePath}:{StartLine}:{StartColumn}" + (RevisionSha is null ? "" : $"@{RevisionSha[..Math.Min(7, RevisionSha.Length)]}");
}

/// <summary>
/// Evidence backing a single graph edge — the concrete syntax that produced the
/// relationship, so "why does A depend on B" always has a real, clickable answer.
/// </summary>
public sealed record EdgeEvidence(
    SourceLocation Location,
    string Snippet,
    double Confidence = 1.0,
    string? Note = null);
