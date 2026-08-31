using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cortex.Build;

/// <summary>
/// Optional Solution Build Inspector: runs `dotnet build` / `dotnet test` in the local
/// workspace under a strict command allowlist (only these two verbs, only inside the
/// registered repository path, no shell interpolation of user input) and parses MSBuild's
/// standard error format so failures can be linked back to graph nodes by file + line.
/// </summary>
public sealed class DotnetProcessRunner
{
    private static readonly Regex MsBuildErrorPattern = new(
        @"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s+(?<severity>error|warning)\s+(?<code>\w+\d*):\s+(?<message>.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public sealed record BuildDiagnostic(string RelativeFilePath, int Line, int Column, bool IsError, string Code, string Message);
    public sealed record BuildResult(bool Succeeded, IReadOnlyList<BuildDiagnostic> Diagnostics, string RawOutput, TimeSpan Duration);

    public async Task<BuildResult> RunAsync(string verb, string workingDirectory, CancellationToken ct)
    {
        if (verb is not ("build" or "test"))
            throw new ArgumentException("Only 'build' and 'test' are permitted by the CORTEX command allowlist.");
        if (!Directory.Exists(workingDirectory))
            throw new DirectoryNotFoundException(workingDirectory);

        var started = DateTimeOffset.UtcNow;
        var psi = new ProcessStartInfo("dotnet", verb)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var combined = stdout + "\n" + stderr;
        var diagnostics = MsBuildErrorPattern.Matches(combined).Select(m => new BuildDiagnostic(
            m.Groups["file"].Value.Replace('\\', '/'),
            int.Parse(m.Groups["line"].Value),
            int.Parse(m.Groups["col"].Value),
            m.Groups["severity"].Value == "error",
            m.Groups["code"].Value,
            m.Groups["message"].Value)).ToList();

        return new BuildResult(process.ExitCode == 0, diagnostics, combined, DateTimeOffset.UtcNow - started);
    }
}
