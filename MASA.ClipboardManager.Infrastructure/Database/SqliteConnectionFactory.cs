using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MASA.ClipboardManager.Infrastructure.Database;

public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var masaFolder = Path.Combine(appData, "MASA.ClipboardManager");
            if (!Directory.Exists(masaFolder))
            {
                Directory.CreateDirectory(masaFolder);
            }
            dbPath = Path.Combine(masaFolder, "clipboard_v1.db");
        }

        _connectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;";
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void InitializeDatabase()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;

            CREATE TABLE IF NOT EXISTS Collections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                ColorHex TEXT NOT NULL,
                Icon TEXT NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ClipboardItems (
                Id TEXT PRIMARY KEY,
                ContentType INTEGER NOT NULL,
                ContentFormat TEXT,
                PlainTextContent TEXT,
                EncryptedPayload TEXT,
                ContentHash TEXT,
                Preview TEXT,
                SearchableText TEXT,
                SizeInBytes INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                LastUsedAt TEXT NOT NULL,
                IsFavorite INTEGER NOT NULL DEFAULT 0,
                IsPinned INTEGER NOT NULL DEFAULT 0,
                IsSensitive INTEGER NOT NULL DEFAULT 0,
                SensitiveType INTEGER NOT NULL DEFAULT 0,
                AutoDeleteAt TEXT,
                CollectionId TEXT,
                ImageStoragePath TEXT,
                SourceProcessName TEXT,
                FileCount INTEGER NOT NULL DEFAULT 0,
                FileExtension TEXT,
                FOREIGN KEY (CollectionId) REFERENCES Collections(Id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Clipboard_CreatedAt ON ClipboardItems(CreatedAt DESC);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_LastUsedAt ON ClipboardItems(LastUsedAt DESC);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_ContentType ON ClipboardItems(ContentType);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_ContentHash ON ClipboardItems(ContentHash);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_IsFavorite ON ClipboardItems(IsFavorite);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_IsPinned ON ClipboardItems(IsPinned);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_CollectionId ON ClipboardItems(CollectionId);
            CREATE INDEX IF NOT EXISTS IX_Clipboard_AutoDeleteAt ON ClipboardItems(AutoDeleteAt);

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
        ";

        cmd.ExecuteNonQuery();

        // Seed default collections if empty
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Collections;";
        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
        if (count == 0)
        {
            using var seedCmd = conn.CreateCommand();
            seedCmd.CommandText = @"
                INSERT INTO Collections (Id, Name, ColorHex, Icon, SortOrder, CreatedAt) VALUES
                ('col_work', 'Work', '#3B82F6', '💼', 1, datetime('now')),
                ('col_dev', 'Development', '#10B981', '💻', 2, datetime('now')),
                ('col_templates', 'Templates', '#F59E0B', '📋', 3, datetime('now')),
                ('col_personal', 'Personal', '#EC4899', '👤', 4, datetime('now'));
            ";
            seedCmd.ExecuteNonQuery();
        }
    }
}
