using Cortex.Core.Abstractions;

namespace Cortex.Git;

/// <summary>
/// Aggregates git-blame lines into per-file, per-author statistics — presented purely as
/// a historical activity summary (Team Knowledge Map / Code Ownership), never as a
/// judgement about individuals.
/// </summary>
public sealed class OwnershipAnalyzer
{
    private readonly IGitProvider _git;
    public OwnershipAnalyzer(IGitProvider git) => _git = git;

    public sealed record OwnershipEntry(string AuthorName, int LineCount, double SharePercent);

    public IReadOnlyList<OwnershipEntry> ComputeForFile(string relativeFilePath)
    {
        var lines = _git.Blame(relativeFilePath);
        var total = lines.Count;
        if (total == 0) return Array.Empty<OwnershipEntry>();

        return lines
            .GroupBy(l => l.AuthorName)
            .Select(g => new OwnershipEntry(g.Key, g.Count(), Math.Round(100.0 * g.Count() / total, 1)))
            .OrderByDescending(e => e.LineCount)
            .ToList();
    }
}
