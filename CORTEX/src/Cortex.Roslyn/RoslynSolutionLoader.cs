using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Cortex.Roslyn;

/// <summary>
/// Loads a .sln or .csproj using the real MSBuild workspace (via Microsoft.Build.Locator,
/// pointed at the developer's installed .NET SDK) so semantic analysis — symbols, type
/// resolution, call graphs — is exact, not a text-based approximation.
/// </summary>
public sealed class RoslynSolutionLoader : IAsyncDisposable
{
    private static bool _registered;
    private MSBuildWorkspace? _workspace;

    public static void EnsureMsBuildRegistered()
    {
        if (_registered) return;
        var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
        var chosen = instances.OrderByDescending(i => i.Version).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No .NET SDK / MSBuild instance found. Install the .NET 10 SDK before running CORTEX indexing.");
        MSBuildLocator.RegisterInstance(chosen);
        _registered = true;
    }

    public async Task<Solution> LoadAsync(string solutionOrProjectPath, IProgress<string>? progress, CancellationToken ct)
    {
        EnsureMsBuildRegistered();
        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += (_, e) => progress?.Report($"[warn] {e.Diagnostic.Message}");

        if (solutionOrProjectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solution = await _workspace.OpenSolutionAsync(solutionOrProjectPath, cancellationToken: ct);
            return solution;
        }

        var project = await _workspace.OpenProjectAsync(solutionOrProjectPath, cancellationToken: ct);
        return project.Solution;
    }

    public ValueTask DisposeAsync()
    {
        _workspace?.Dispose();
        return ValueTask.CompletedTask;
    }
}
