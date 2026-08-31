using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Git;

/// <summary>
/// Maps the files touched by a single commit onto the Code Knowledge Graph, so the
/// Commit Timeline and Diff-to-Graph view can highlight exactly which nodes/edges were
/// affected, then hands off to <see cref="IImpactAnalyzer"/> for the downstream blast radius.
/// </summary>
public sealed class CommitImpactAnalyzer
{
    private readonly IGitProvider _git;
    private readonly ICodeGraphEngine _graph;

    public CommitImpactAnalyzer(IGitProvider git, ICodeGraphEngine graph)
    {
        _git = git;
        _graph = graph;
    }

    public sealed record CommitGraphImpact(
        CommitInfo Commit, IReadOnlyList<ChangedFile> ChangedFiles, IReadOnlyList<GraphNode> DirectlyTouchedNodes);

    public CommitGraphImpact Analyze(string repositoryId, string commitSha, IReadOnlyList<CommitInfo> historyLookup)
    {
        var commit = historyLookup.First(c => c.Sha == commitSha);
        var changedFiles = _git.GetChangedFiles(commitSha);

        var touchedNodes = changedFiles
            .SelectMany(cf => _graph.GetAllNodes(repositoryId)
                .Where(n => n.Location is not null &&
                            n.Location.RelativeFilePath.Equals(cf.RelativePath, StringComparison.OrdinalIgnoreCase)))
            .DistinctBy(n => n.Id)
            .ToList();

        return new CommitGraphImpact(commit, changedFiles, touchedNodes);
    }
}
