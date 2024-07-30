using Microsoft.Data.Sqlite;
using System;

public class DBLite
{
    private readonly string _connectionString;

    public DBLite(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS hashes (
                sha256 TEXT PRIMARY KEY,
                md5 TEXT,
                sha1 TEXT,
                file_size INTEGER,
                last_seen TEXT,
                scanned INTEGER DEFAULT 0
            );";
        command.ExecuteNonQuery();

        var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS FileCache (
                FilePath TEXT PRIMARY KEY
            );";
        tableCommand.ExecuteNonQuery();
    }

    public void InsertOrUpdateFile(string sha256, string md5, string sha1, long fileSize)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO hashes (sha256, md5, sha1, file_size, last_seen, scanned)
            VALUES ($sha256, $md5, $sha1, $fileSize, $lastSeen, $scanned);";
        command.Parameters.AddWithValue("$sha256", sha256);
        command.Parameters.AddWithValue("$md5", md5);
        command.Parameters.AddWithValue("$sha1", sha1);
        command.Parameters.AddWithValue("$fileSize", fileSize);
        command.Parameters.AddWithValue("$lastSeen", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("$scanned", 1);

        command.ExecuteNonQuery();
    }

    public bool IsFileCached(string filePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT COUNT(*) FROM FileCache WHERE FilePath = @filePath";
        selectCommand.Parameters.AddWithValue("@filePath", filePath);

        var count = (long)selectCommand.ExecuteScalar();
        return count > 0;
    }

    public bool Increment(string sha256Hash)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
        UPDATE hashes
        SET scanned = scanned + 1,
            last_seen = @lastSeen
        WHERE sha256 = @sha256Hash;
    ";
        command.Parameters.AddWithValue("@sha256Hash", sha256Hash);
        command.Parameters.AddWithValue("@lastSeen", DateTime.UtcNow.ToString("o"));

        // Execute the update command
        var count = command.ExecuteNonQuery();
        return count > 0;
    }

    public void AddFileToCache(string filePath)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO FileCache (FilePath) VALUES (@filePath)";
        insertCommand.Parameters.AddWithValue("@filePath", filePath);
        insertCommand.ExecuteNonQuery();
    }
}
