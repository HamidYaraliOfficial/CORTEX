using System.Security.Cryptography;

namespace Cortex.Indexing;

/// <summary>
/// Tracks a SHA-256 content hash + last-write-time per file so the Incremental Indexer
/// can decide, on every scan, exactly which files actually changed since the last index —
/// without this, every re-index would have to be a full repository re-scan.
/// </summary>
public sealed class FileHashTracker
{
    private readonly Dictionary<string, (string Hash, DateTimeOffset LastWriteUtc)> _known = new();

    public void LoadKnownState(IEnumerable<(string RelativePath, string Hash, DateTimeOffset LastWriteUtc)> persisted)
    {
        foreach (var (path, hash, when) in persisted) _known[path] = (hash, when);
    }

    public IReadOnlyDictionary<string, (string Hash, DateTimeOffset LastWriteUtc)> Snapshot() => _known;

    public sealed record ChangeSet(IReadOnlyList<string> Added, IReadOnlyList<string> Modified, IReadOnlyList<string> Deleted, IReadOnlyList<string> Unchanged);

    public async Task<ChangeSet> DetectChangesAsync(string repositoryRoot, IReadOnlyList<string> currentRelativeFiles, CancellationToken ct)
    {
        var added = new List<string>();
        var modified = new List<string>();
        var unchanged = new List<string>();
        var seen = new HashSet<string>();

        foreach (var relativePath in currentRelativeFiles)
        {
            ct.ThrowIfCancellationRequested();
            seen.Add(relativePath);
            var fullPath = Path.Combine(repositoryRoot, relativePath);
            var info = new FileInfo(fullPath);
            if (!info.Exists) continue;

            if (!_known.TryGetValue(relativePath, out var previous))
            {
                var hash = await ComputeHashAsync(fullPath, ct);
                _known[relativePath] = (hash, info.LastWriteTimeUtc);
                added.Add(relativePath);
                continue;
            }

            // Cheap check first (mtime), only re-hash when it actually moved.
            if (previous.LastWriteUtc == info.LastWriteTimeUtc)
            {
                unchanged.Add(relativePath);
                continue;
            }

            var newHash = await ComputeHashAsync(fullPath, ct);
            if (newHash == previous.Hash)
            {
                unchanged.Add(relativePath);
            }
            else
            {
                _known[relativePath] = (newHash, info.LastWriteTimeUtc);
                modified.Add(relativePath);
            }
        }

        var deleted = _known.Keys.Where(k => !seen.Contains(k)).ToList();
        foreach (var d in deleted) _known.Remove(d);

        return new ChangeSet(added, modified, deleted, unchanged);
    }

    private static async Task<string> ComputeHashAsync(string fullPath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(fullPath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }
}
