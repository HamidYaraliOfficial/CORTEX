using System.Text.RegularExpressions;

namespace Cortex.Security;

/// <summary>
/// Best-effort, pattern-based detector for accidentally-committed secrets (cloud provider
/// key shapes, generic high-entropy tokens, connection strings). Results are always shown
/// as "Potential Secret" candidates with an explicit false-positive warning — this scanner
/// never collects, transmits, or stores the actual secret value it finds.
/// </summary>
public static class SecretPatternScanner
{
    public sealed record PotentialSecret(string RelativeFilePath, int LineNumber, string PatternName, string MaskedPreview);

    private static readonly (string Name, Regex Pattern)[] Patterns =
    {
        ("AWS Access Key", new Regex("AKIA[0-9A-Z]{16}")),
        ("Generic API Key Assignment", new Regex("(?i)(api[_-]?key|secret|token)\\s*[:=]\\s*['\"][A-Za-z0-9_\\-]{16,}['\"]")),
        ("Private Key Block", new Regex("-----BEGIN (RSA|EC|OPENSSH|PGP) PRIVATE KEY-----")),
        ("SQL Connection String w/ Password", new Regex("(?i)password\\s*=\\s*[^;'\"\\s]{4,}")),
        ("Slack Token", new Regex("xox[baprs]-[0-9A-Za-z-]{10,}")),
    };

    public static IReadOnlyList<PotentialSecret> Scan(string relativeFilePath, string content)
    {
        var findings = new List<PotentialSecret>();
        var lines = content.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var (name, pattern) in Patterns)
            {
                var match = pattern.Match(lines[i]);
                if (!match.Success) continue;
                var masked = match.Value.Length <= 8 ? "****" : $"{match.Value[..4]}...{match.Value[^2..]}";
                findings.Add(new PotentialSecret(relativeFilePath, i + 1, name, masked));
            }
        }

        return findings;
    }
}
