using System.Text.Json;
using F2Share.Application.Abstractions;
using F2Share.Domain.ValueObjects;
using Microsoft.Data.Sqlite;

namespace F2Share.Infrastructure.Persistence;

public sealed class SqliteMetadataStore : IMetadataStore
{
    private readonly string _connectionString;

    public SqliteMetadataStore(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? ".");
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schema.sql");
        if (!File.Exists(schemaPath))
        {
            schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Persistence", "Schema.sql");
        }

        var sql = File.Exists(schemaPath)
            ? await File.ReadAllTextAsync(schemaPath, cancellationToken).ConfigureAwait(false)
            : "CREATE TABLE IF NOT EXISTS file_fingerprints (share_id TEXT NOT NULL, relative_path TEXT NOT NULL, length INTEGER NOT NULL, last_write_utc TEXT NOT NULL, strong_hash TEXT NOT NULL, block_hashes TEXT NOT NULL, is_deleted INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (share_id, relative_path));";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FileFingerprint?> GetFingerprintAsync(string shareId, string relativePath, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT relative_path, length, last_write_utc, strong_hash, block_hashes
                                FROM file_fingerprints
                                WHERE share_id = $share_id AND relative_path = $relative_path AND is_deleted = 0;";
        command.Parameters.AddWithValue("$share_id", shareId);
        command.Parameters.AddWithValue("$relative_path", relativePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var blockHashes = JsonSerializer.Deserialize<List<string>>(reader.GetString(4)) ?? [];
        return new FileFingerprint(
            reader.GetString(0),
            reader.GetInt64(1),
            DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.GetString(3),
            blockHashes);
    }

    public async Task UpsertFingerprintAsync(string shareId, FileFingerprint fingerprint, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO file_fingerprints (share_id, relative_path, length, last_write_utc, strong_hash, block_hashes, is_deleted)
                                VALUES ($share_id, $relative_path, $length, $last_write_utc, $strong_hash, $block_hashes, 0)
                                ON CONFLICT(share_id, relative_path) DO UPDATE SET
                                    length = excluded.length,
                                    last_write_utc = excluded.last_write_utc,
                                    strong_hash = excluded.strong_hash,
                                    block_hashes = excluded.block_hashes,
                                    is_deleted = 0;";

        command.Parameters.AddWithValue("$share_id", shareId);
        command.Parameters.AddWithValue("$relative_path", fingerprint.RelativePath);
        command.Parameters.AddWithValue("$length", fingerprint.Length);
        command.Parameters.AddWithValue("$last_write_utc", fingerprint.LastWriteUtc.ToString("O"));
        command.Parameters.AddWithValue("$strong_hash", fingerprint.StrongHash);
        command.Parameters.AddWithValue("$block_hashes", JsonSerializer.Serialize(fingerprint.BlockHashes));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkDeletedAsync(string shareId, string relativePath, DateTimeOffset atUtc, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE file_fingerprints
                                SET is_deleted = 1, last_write_utc = $last_write_utc
                                WHERE share_id = $share_id AND relative_path = $relative_path;";
        command.Parameters.AddWithValue("$share_id", shareId);
        command.Parameters.AddWithValue("$relative_path", relativePath);
        command.Parameters.AddWithValue("$last_write_utc", atUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FileFingerprint>> ListFingerprintsAsync(string shareId, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT relative_path, length, last_write_utc, strong_hash, block_hashes
                                FROM file_fingerprints
                                WHERE share_id = $share_id AND is_deleted = 0;";
        command.Parameters.AddWithValue("$share_id", shareId);

        var list = new List<FileFingerprint>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new FileFingerprint(
                reader.GetString(0),
                reader.GetInt64(1),
                DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
                reader.GetString(3),
                JsonSerializer.Deserialize<List<string>>(reader.GetString(4)) ?? []));
        }

        return list;
    }
}
