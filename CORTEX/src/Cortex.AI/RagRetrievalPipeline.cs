using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Graph;

namespace Cortex.AI;

/// <summary>
/// Tool-based retrieval: instead of stuffing the whole repository into one giant prompt,
/// the assistant answers a question by calling small, precise tools — symbol search,
/// graph traversal, git history — and only the results of those calls become the grounded
/// context handed to the language model. This keeps answers traceable to real source
/// locations and keeps token cost roughly constant regardless of repository size.
/// </summary>
public sealed class RagRetrievalPipeline
{
    private readonly ISearchEngine _search;
    private readonly ICodeGraphEngine _graph;
    private readonly GraphQueryEngine _graphQuery;
    private readonly AiPermissionScope _permissions;

    public RagRetrievalPipeline(ISearchEngine search, ICodeGraphEngine graph, AiPermissionScope permissions)
    {
        _search = search;
        _graph = graph;
        _graphQuery = new GraphQueryEngine(graph);
        _permissions = permissions;
    }

    public sealed record RetrievedContext(string SourceDescription, string Text, SourceLocation? Location);

    public async Task<IReadOnlyList<RetrievedContext>> RetrieveAsync(string repositoryId, string question, CancellationToken ct)
    {
        if (!_permissions.CanAccessRepository(repositoryId))
            throw new UnauthorizedAccessException("The AI assistant does not have permission to access this repository. Grant access in the Security/Privacy Center.");

        var results = new List<RetrievedContext>();

        // Tool 1: deterministic graph query, for questions that map onto a known relationship shape.
        var graphAnswer = _graphQuery.Ask(repositoryId, question);
        if (graphAnswer.Matched)
        {
            foreach (var node in graphAnswer.Nodes)
            {
                if (node.Location is not null && !_permissions.CanAccessFile(node.Location.RelativeFilePath)) continue;
                results.Add(new RetrievedContext($"graph:{graphAnswer.Edge}", $"{node.Kind} {node.FullyQualifiedName}", node.Location));
            }
        }

        // Tool 2: full-text symbol search, as a fallback / complement for anything the graph pattern missed.
        var hits = await _search.SearchAsync(repositoryId, question, maxResults: 8, ct);
        foreach (var hit in hits)
        {
            results.Add(new RetrievedContext($"search:{hit.Kind}", $"{hit.Title} — {hit.Snippet}", Location: null));
        }

        return results;
    }
}

/// <summary>
/// Orchestrates one AI Codebase Assistant turn: retrieve grounded context via
/// <see cref="RagRetrievalPipeline"/>, then ask the configured <see cref="IAiProvider"/>
/// to answer strictly from that context, never from an ungrounded whole-repo dump.
/// </summary>
public sealed class AiCodebaseAssistant
{
    private readonly RagRetrievalPipeline _retrieval;
    private readonly IAiProvider _provider;

    private const string SystemPrompt =
        "You are the CORTEX Codebase Assistant. Answer only using the retrieved repository " +
        "context provided below. Cite file paths for every claim. If the context is insufficient, say so explicitly.";

    public AiCodebaseAssistant(RagRetrievalPipeline retrieval, IAiProvider provider)
    {
        _retrieval = retrieval;
        _provider = provider;
    }

    public async Task<string> AskAsync(string repositoryId, string question, CancellationToken ct)
    {
        var context = await _retrieval.RetrieveAsync(repositoryId, question, ct);
        var contextTexts = context.Select(c => $"[{c.SourceDescription}{(c.Location is null ? "" : $" @ {c.Location}")}] {c.Text}").ToList();
        return await _provider.CompleteAsync(SystemPrompt, question, contextTexts, ct);
    }
}
