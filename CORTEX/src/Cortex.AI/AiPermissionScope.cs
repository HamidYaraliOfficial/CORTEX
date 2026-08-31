namespace Cortex.AI;

/// <summary>
/// Explicit allow-list of what the AI Codebase Assistant may read for a given question.
/// By default AI features are local-only and repository data never leaves the machine;
/// Cloud AI providers must be turned on deliberately per workspace (see Security/Privacy Center).
/// </summary>
public sealed class AiPermissionScope
{
    public bool CloudProviderEnabled { get; set; } = false;
    public HashSet<string> AllowedRepositoryIds { get; } = new();
    public HashSet<string> DeniedRelativeFilePathGlobs { get; } = new() { "**/*.env", "**/appsettings*.json", "**/*.pfx", "**/secrets.json" };

    public bool CanAccessRepository(string repositoryId) => AllowedRepositoryIds.Contains(repositoryId);

    public bool CanAccessFile(string relativeFilePath) =>
        !DeniedRelativeFilePathGlobs.Any(glob => System.Text.RegularExpressions.Regex.IsMatch(
            relativeFilePath,
            "^" + System.Text.RegularExpressions.Regex.Escape(glob).Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase));
}
