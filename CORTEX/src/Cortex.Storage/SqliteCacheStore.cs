using Microsoft.Data.Sqlite;

namespace Cortex.Storage;

/// <summary>
/// Small generic key/value cache (parsed-AST fragments, git metadata, graph fragments)
/// invalidated by repository revision SHA or file content hash — whichever the caller
/// supplies as <paramref name="invalidationKey"/> on write/read.
/// </summary>
public sealed class SqliteCacheStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteCacheStore(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS cache_entries (
                cache_key TEXT PRIMARY KEY,
                invalidation_key TEXT NOT NULL,
                payload BLOB NOT NULL,
                written_at_utc TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<byte[]?> TryGetAsync(string cacheKey, string currentInvalidationKey, CancellationToken ct)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT payload, invalidation_key FROM cache_entries WHERE cache_key = $k;";
        cmd.Parameters.AddWithValue("$k", cacheKey);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var storedInvalidationKey = reader.GetString(1);
        if (storedInvalidationKey != currentInvalidationKey) return null; // stale — file/revision moved on
        return (byte[])reader["payload"];
    }

    public async Task SetAsync(string cacheKey, string invalidationKey, byte[] payload, CancellationToken ct)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO cache_entries (cache_key, invalidation_key, payload, written_at_utc)
            VALUES ($k, $i, $p, $t)
            ON CONFLICT(cache_key) DO UPDATE SET invalidation_key = $i, payload = $p, written_at_utc = $t;
            """;
        cmd.Parameters.AddWithValue("$k", cacheKey);
        cmd.Parameters.AddWithValue("$i", invalidationKey);
        cmd.Parameters.AddWithValue("$p", payload);
        cmd.Parameters.AddWithValue("$t", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public void Dispose() => _connection.Dispose();
}
