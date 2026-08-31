using Cortex.Core.Abstractions;

namespace Cortex.Git;

/// <summary>Counts how often each file changed across recent history — the raw signal for Code Churn hotspots.</summary>
public sealed class ChurnAnalyzer
{
    private readonly IGitProvider _git;
    public ChurnAnalyzer(IGitProvider git) => _git = git;

    public sealed record ChurnEntry(string RelativeFilePath, int ChangeCount, DateTimeOffset LastChangedUtc);

    public IReadOnlyList<ChurnEntry> Compute(string? branch, int commitWindow = 500)
    {
        var commits = _git.GetCommitHistory(branch, commitWindow);
        var counts = new Dictionary<string, (int Count, DateTimeOffset Last)>();

        foreach (var commit in commits)
        {
            foreach (var file in _git.GetChangedFiles(commit.Sha))
            {
                var existing = counts.TryGetValue(file.RelativePath, out var v) ? v : (0, commit.When);
                counts[file.RelativePath] = (existing.Item1 + 1, existing.Item2 > commit.When ? existing.Item2 : commit.When);
            }
        }

        return counts
            .Select(kv => new ChurnEntry(kv.Key, kv.Value.Count, kv.Value.Last))
            .OrderByDescending(e => e.ChangeCount)
            .ToList();
    }
}
