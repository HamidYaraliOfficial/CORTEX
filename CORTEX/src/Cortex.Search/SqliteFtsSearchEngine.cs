using Cortex.Core.Abstractions;
using Microsoft.Data.Sqlite;

namespace Cortex.Search;

/// <summary>
/// Full-text symbol/file search backed by SQLite's FTS5 virtual table — fast, local,
/// no external search service required. One FTS5 table per workspace database file
/// (see Cortex.Storage), scoped per repository via a stored column.
/// </summary>
public sealed class SqliteFtsSearchEngine : ISearchEngine, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteFtsSearchEngine(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath}");
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS symbol_search USING fts5(
                repository_id UNINDEXED,
                document_id UNINDEXED,
                title,
                body,
                kind UNINDEXED,
                tokenize = 'porter unicode61'
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task IndexDocumentAsync(string repositoryId, string documentId, string title, string body, string kind, CancellationToken ct)
    {
        await RemoveDocumentAsync(repositoryId, documentId, ct);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO symbol_search (repository_id, document_id, title, body, kind) VALUES ($r, $d, $t, $b, $k);";
        cmd.Parameters.AddWithValue("$r", repositoryId);
        cmd.Parameters.AddWithValue("$d", documentId);
        cmd.Parameters.AddWithValue("$t", title);
        cmd.Parameters.AddWithValue("$b", body);
        cmd.Parameters.AddWithValue("$k", kind);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveDocumentAsync(string repositoryId, string documentId, CancellationToken ct)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM symbol_search WHERE repository_id = $r AND document_id = $d;";
        cmd.Parameters.AddWithValue("$r", repositoryId);
        cmd.Parameters.AddWithValue("$d", documentId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string repositoryId, string query, int maxResults, CancellationToken ct)
    {
        // Fall back to a prefix/fuzzy-friendly match if the raw query isn't valid FTS5 syntax.
        var ftsQuery = string.Join(" OR ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => $"{t}*"));
        if (string.IsNullOrWhiteSpace(ftsQuery)) return Array.Empty<SearchHit>();

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT document_id, title, kind, snippet(symbol_search, 3, '[', ']', '...', 8) AS snip, bm25(symbol_search) AS score
            FROM symbol_search
            WHERE repository_id = $r AND symbol_search MATCH $q
            ORDER BY score LIMIT $n;
            """;
        cmd.Parameters.AddWithValue("$r", repositoryId);
        cmd.Parameters.AddWithValue("$q", ftsQuery);
        cmd.Parameters.AddWithValue("$n", maxResults);

        var results = new List<SearchHit>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new SearchHit(
                reader.GetString(0), reader.GetString(1), reader.GetString(3), reader.GetString(2),
                Score: -reader.GetDouble(4))); // bm25 is "lower is better"; invert for "higher is better" in the UI
        }
        return results;
    }

    public void Dispose() => _connection.Dispose();
}
