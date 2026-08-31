using Cortex.Core.Models;

namespace Cortex.Core.Abstractions;

/// <summary>In-memory + persisted store of the Code Knowledge Graph for one repository.</summary>
public interface ICodeGraphEngine
{
    void AddNode(GraphNode node);
    void AddEdge(GraphEdge edge);
    void RemoveNodesFromFile(string repositoryId, string relativeFilePath);
    GraphNode? GetNode(string nodeId);
    IEnumerable<GraphNode> GetAllNodes(string repositoryId);
    IEnumerable<GraphEdge> GetOutgoingEdges(string nodeId, EdgeType? filter = null);
    IEnumerable<GraphEdge> GetIncomingEdges(string nodeId, EdgeType? filter = null);
    IReadOnlyList<GraphNode> FindByName(string repositoryId, string nameFragment, int maxResults = 50);
}

/// <summary>Roslyn-backed static analyzer that turns a solution/project into graph facts.</summary>
public interface IRoslynAnalyzer
{
    Task<AnalysisResult> AnalyzeSolutionAsync(string solutionOrProjectPath, IProgress<AnalysisProgress>? progress, CancellationToken ct);
    Task<AnalysisResult> AnalyzeFileAsync(string repositoryId, string absoluteFilePath, CancellationToken ct);
}

public sealed record AnalysisProgress(string CurrentFile, int FilesProcessed, int TotalFiles);
public sealed record AnalysisResult(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges, IReadOnlyList<string> DiagnosticsSummary);

/// <summary>Git history / metadata provider, backed by LibGit2Sharp.</summary>
public interface IGitProvider
{
    IReadOnlyList<string> GetBranches();
    IReadOnlyList<string> GetTags();
    IReadOnlyList<CommitInfo> GetCommitHistory(string? branch, int maxCount);
    IReadOnlyList<ChangedFile> GetChangedFiles(string commitSha);
    IReadOnlyList<ChangedFile> Diff(string fromRevision, string toRevision);
    IReadOnlyList<BlameLine> Blame(string relativeFilePath);
}

public sealed record CommitInfo(string Sha, string AuthorName, string AuthorEmail, DateTimeOffset When, string Message);
public sealed record ChangedFile(string RelativePath, ChangeKind Kind, int LinesAdded, int LinesDeleted);
public enum ChangeKind { Added, Modified, Deleted, Renamed }
public sealed record BlameLine(int LineNumber, string AuthorName, string CommitSha, DateTimeOffset When);

/// <summary>Full-text / symbol / fuzzy search over one repository's indexed content.</summary>
public interface ISearchEngine
{
    Task IndexDocumentAsync(string repositoryId, string documentId, string title, string body, string kind, CancellationToken ct);
    Task RemoveDocumentAsync(string repositoryId, string documentId, CancellationToken ct);
    Task<IReadOnlyList<SearchHit>> SearchAsync(string repositoryId, string query, int maxResults, CancellationToken ct);
}

public sealed record SearchHit(string DocumentId, string Title, string Snippet, string Kind, double Score);

/// <summary>Pluggable AI provider — local (ONNX) or cloud, behind one contract.</summary>
public interface IAiProvider
{
    AiProviderKind Kind { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, IReadOnlyList<string> retrievedContext, CancellationToken ct);
}

/// <summary>Evaluates user-defined architecture rules against the current graph.</summary>
public interface IRuleEngine
{
    IReadOnlyList<RuleViolation> Evaluate(string repositoryId, IReadOnlyList<ArchitectureRule> rules);
}

public sealed record ArchitectureRule(string Id, string Name, string SourcePattern, string ForbiddenTargetPattern, RuleSeverity Severity, bool Enabled = true);
public sealed record RuleViolation(ArchitectureRule Rule, GraphEdge OffendingEdge, string Explanation);

public interface IMetricsEngine
{
    ModuleMetrics ComputeForFile(string repositoryId, string relativeFilePath, string sourceText);
    ArchitectureHealth ComputeHealthScore(string repositoryId, IReadOnlyList<ModuleMetrics> allMetrics, IReadOnlyList<RuleViolation> violations, int circularDependencyCount);
}

public sealed record ModuleMetrics(
    string RelativeFilePath, int LinesOfCode, int CyclomaticComplexity, int MethodCount,
    int ClassCount, int FanIn, int FanOut, double CouplingProxy);

public sealed record ArchitectureHealth(int Score0To100, IReadOnlyList<string> ContributingFactors, string Disclaimer =
    "This score is an analytical indicator derived from static heuristics — not a certified measure of code quality.");

/// <summary>The heart of CORTEX: what breaks if this changes?</summary>
public interface IImpactAnalyzer
{
    ImpactReport AnalyzeNodeImpact(string repositoryId, string nodeId, ImpactOptions options);
    ImpactReport AnalyzeCommitImpact(string repositoryId, string commitSha);
}

public sealed record ImpactOptions(bool IncludeIndirect = true, int MaxDepth = 6, bool IncludeTests = true, bool IncludeConfiguration = true);

public sealed record ImpactReport(
    string RootNodeId, ImpactLevel Level, IReadOnlyList<ImpactedItem> DirectlyImpacted,
    IReadOnlyList<ImpactedItem> IndirectlyImpacted, IReadOnlyList<ImpactedItem> AffectedTests,
    IReadOnlyList<ImpactedItem> AffectedApiContracts, string Summary);

public sealed record ImpactedItem(string NodeId, string DisplayName, NodeType Kind, EdgeType RelationToRoot, int Depth);

public interface ICredentialStore
{
    void Save(string key, string secret);
    string? TryRead(string key);
    void Delete(string key);
}

public interface IAuditLogger
{
    void Record(string category, string action, string? repositoryId, IReadOnlyDictionary<string, string>? metadata = null);
}

public interface IJobScheduler
{
    string Enqueue(string jobName, Func<IProgress<JobProgress>, CancellationToken, Task> work);
    JobStatus? GetStatus(string jobId);
    void Cancel(string jobId);
    IReadOnlyList<JobStatus> GetAllJobs();
}

public sealed record JobProgress(double PercentComplete, string Message);
public enum JobState { Queued, Running, Completed, Failed, Cancelled }
public sealed record JobStatus(string Id, string Name, JobState State, double PercentComplete, string LastMessage, DateTimeOffset StartedAtUtc, DateTimeOffset? FinishedAtUtc, string? Error);
