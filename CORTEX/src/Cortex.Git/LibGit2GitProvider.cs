using Cortex.Core.Abstractions;
using LibGit2Sharp;

namespace Cortex.Git;

/// <summary>
/// Read-only Git intelligence layer built on LibGit2Sharp. CORTEX never performs
/// destructive operations (no force-push, no remote rewrite, no branch deletion) —
/// this class only ever reads history, diffs and blame.
/// </summary>
public sealed class LibGit2GitProvider : IGitProvider, IDisposable
{
    private readonly Repository _repo;

    public LibGit2GitProvider(string repositoryPath)
    {
        var discovered = Repository.Discover(repositoryPath)
            ?? throw new InvalidOperationException($"'{repositoryPath}' is not inside a Git repository.");
        _repo = new Repository(discovered);
    }

    public IReadOnlyList<string> GetBranches() =>
        _repo.Branches.Where(b => !b.IsRemote).Select(b => b.FriendlyName).ToList();

    public IReadOnlyList<string> GetTags() =>
        _repo.Tags.Select(t => t.FriendlyName).ToList();

    public IReadOnlyList<CommitInfo> GetCommitHistory(string? branch, int maxCount)
    {
        var tip = branch is null ? _repo.Head.Tip : _repo.Branches[branch]?.Tip;
        if (tip is null) return Array.Empty<CommitInfo>();

        return _repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = tip })
            .Take(maxCount)
            .Select(c => new CommitInfo(c.Sha, c.Author.Name, c.Author.Email, c.Author.When, c.MessageShort))
            .ToList();
    }

    public IReadOnlyList<ChangedFile> GetChangedFiles(string commitSha)
    {
        var commit = _repo.Lookup<Commit>(commitSha) ?? throw new ArgumentException($"Unknown commit {commitSha}");
        var parent = commit.Parents.FirstOrDefault();
        var comparison = parent is null
            ? _repo.Diff.Compare<TreeChanges>(null, commit.Tree)
            : _repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);

        return comparison.Select(ToChangedFile).ToList();
    }

    public IReadOnlyList<ChangedFile> Diff(string fromRevision, string toRevision)
    {
        var from = _repo.Lookup<Commit>(fromRevision) ?? throw new ArgumentException($"Unknown revision {fromRevision}");
        var to = _repo.Lookup<Commit>(toRevision) ?? throw new ArgumentException($"Unknown revision {toRevision}");
        var comparison = _repo.Diff.Compare<TreeChanges>(from.Tree, to.Tree);
        return comparison.Select(ToChangedFile).ToList();
    }

    public IReadOnlyList<BlameLine> Blame(string relativeFilePath)
    {
        var blame = _repo.Blame(relativeFilePath);
        var results = new List<BlameLine>();
        foreach (var hunk in blame)
        {
            for (var line = hunk.FinalStartLineNumber; line < hunk.FinalStartLineNumber + hunk.LineCount; line++)
            {
                results.Add(new BlameLine(line + 1, hunk.FinalSignature.Name, hunk.FinalCommit.Sha, hunk.FinalSignature.When));
            }
        }
        return results;
    }

    private static ChangedFile ToChangedFile(TreeEntryChanges change)
    {
        var kind = change.Status switch
        {
            ChangeKind.Added => Core.Abstractions.ChangeKind.Added,
            ChangeKind.Deleted => Core.Abstractions.ChangeKind.Deleted,
            ChangeKind.Renamed => Core.Abstractions.ChangeKind.Renamed,
            _ => Core.Abstractions.ChangeKind.Modified
        };
        return new ChangedFile(change.Path, kind, LinesAdded: 0, LinesDeleted: 0);
    }

    public void Dispose() => _repo.Dispose();
}
