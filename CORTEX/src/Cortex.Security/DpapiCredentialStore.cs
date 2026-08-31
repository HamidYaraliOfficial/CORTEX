using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Cortex.Core.Abstractions;

namespace Cortex.Security;

/// <summary>
/// Stores Git tokens and Cloud AI API keys encrypted at rest with Windows DPAPI,
/// scoped to the current Windows user (<see cref="DataProtectionScope.CurrentUser"/>),
/// so the ciphertext is useless outside this Windows account even if the file is copied.
/// Secrets are written under %LOCALAPPDATA%\CORTEX\credentials and never logged.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiCredentialStore : ICredentialStore
{
    private readonly string _storageDirectory;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Cortex.Security.v1");

    public DpapiCredentialStore(string? storageDirectory = null)
    {
        _storageDirectory = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CORTEX", "credentials");
        Directory.CreateDirectory(_storageDirectory);
    }

    public void Save(string key, string secret)
    {
        var plainBytes = Encoding.UTF8.GetBytes(secret);
        var protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PathFor(key), protectedBytes);
    }

    public string? TryRead(string key)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;
        var protectedBytes = File.ReadAllBytes(path);
        var plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public void Delete(string key)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
    }

    private string PathFor(string key)
    {
        var safeName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..32].ToLowerInvariant();
        return Path.Combine(_storageDirectory, $"{safeName}.cred");
    }
}
