using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Roslyn;
using Cortex.Search;

namespace Cortex.Indexing;

/// <summary>
/// Orchestrates a full index (first import) or an incremental re-index (after files
/// change) of one repository: detect changed files → re-run Roslyn only on those →
/// remove stale graph nodes for changed/deleted files → merge new facts into the graph →
/// update the full-text search index. Designed so a multi-million-line repository only
/// pays the cost of the files that actually moved.
/// </summary>
public sealed class IncrementalIndexer
{
    private readonly ICodeGraphEngine _graph;
    private readonly ISearchEngine _search;
    private readonly FileHashTracker _hashTracker = new();

    public IncrementalIndexer(ICodeGraphEngine graph, ISearchEngine search)
    {
        _graph = graph;
        _search = search;
    }

    public sealed record IndexRunResult(int FilesScanned, int FilesReanalyzed, int NodesAdded, int EdgesAdded, TimeSpan Duration);

    public async Task<IndexRunResult> RunAsync(
        string repositoryId, string repositoryRoot, string solutionOrProjectPath,
        IProgress<AnalysisProgress>? progress, CancellationToken ct)
    {
        var started = DateTimeOffset.UtcNow;
        var allSourceFiles = Directory.EnumerateFiles(repositoryRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => Path.GetRelativePath(repositoryRoot, f))
            .ToList();

        var changes = await _hashTracker.DetectChangesAsync(repositoryRoot, allSourceFiles, ct);
        var toReanalyze = changes.Added.Concat(changes.Modified).ToList();

        foreach (var deleted in changes.Deleted)
        {
            _graph.RemoveNodesFromFile(repositoryId, deleted);
            await _search.RemoveDocumentAsync(repositoryId, deleted, ct);
        }
        foreach (var modified in changes.Modified)
        {
            _graph.RemoveNodesFromFile(repositoryId, modified);
        }

        var nodesAdded = 0;
        var edgesAdded = 0;

        if (toReanalyze.Count > 0)
        {
            await using var loader = new RoslynSolutionLoader();
            var solution = await loader.LoadAsync(solutionOrProjectPath, new Progress<string>(_ => { }), ct);
            var builder = new SymbolGraphBuilder();
            var result = await builder.BuildAsync(repositoryId, solution, progress, ct);

            foreach (var node in result.Nodes)
            {
                _graph.AddNode(node);
                nodesAdded++;
                if (node.Location is not null)
                {
                    await _search.IndexDocumentAsync(repositoryId, node.Id, node.DisplayName,
                        $"{node.FullyQualifiedName} {node.Kind}", node.Kind.ToString(), ct);
                }
            }
            foreach (var edge in result.Edges)
            {
                _graph.AddEdge(edge);
                edgesAdded++;
            }
        }

        return new IndexRunResult(allSourceFiles.Count, toReanalyze.Count, nodesAdded, edgesAdded, DateTimeOffset.UtcNow - started);
    }
}
