using System.Text.Json;
using Cortex.Core.Abstractions;

namespace Cortex.Security;

/// <summary>
/// Append-only, local audit trail for security-relevant events (repository added/removed,
/// AI provider changed, export performed, credential accessed, rule changed). Deliberately
/// records only metadata — never file contents, secrets, or source code — so the log itself
/// can never become a data leak.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly string _logFilePath;
    private static readonly object WriteLock = new();

    public AuditLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
    }

    public void Record(string category, string action, string? repositoryId, IReadOnlyDictionary<string, string>? metadata = null)
    {
        var entry = new
        {
            atUtc = DateTimeOffset.UtcNow,
            category,
            action,
            repositoryId,
            metadata = metadata ?? new Dictionary<string, string>()
        };
        var line = JsonSerializer.Serialize(entry);
        lock (WriteLock) File.AppendAllLines(_logFilePath, new[] { line });
    }
}
