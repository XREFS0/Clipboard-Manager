using System.Text.Json;
using Dapper;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;
using MASA.ClipboardManager.Infrastructure.Database;

namespace MASA.ClipboardManager.Infrastructure.Repositories;

public class CollectionRepository : ICollectionRepository
{
    private readonly SqliteConnectionFactory _factory;

    public CollectionRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<CollectionItem>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT c.*, (SELECT COUNT(*) FROM ClipboardItems ci WHERE ci.CollectionId = c.Id) AS ItemCount
            FROM Collections c
            ORDER BY c.SortOrder ASC, c.Name ASC;";
        return await conn.QueryAsync<CollectionItem>(sql);
    }

    public async Task<CollectionItem?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT c.*, (SELECT COUNT(*) FROM ClipboardItems ci WHERE ci.CollectionId = c.Id) AS ItemCount
            FROM Collections c
            WHERE c.Id = @Id;";
        return await conn.QuerySingleOrDefaultAsync<CollectionItem>(sql, new { Id = id });
    }

    public async Task<int> AddAsync(CollectionItem collection)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            INSERT INTO Collections (Id, Name, ColorHex, Icon, SortOrder, CreatedAt)
            VALUES (@Id, @Name, @ColorHex, @Icon, @SortOrder, @CreatedAt);";
        return await conn.ExecuteAsync(sql, collection);
    }

    public async Task<int> UpdateAsync(CollectionItem collection)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            UPDATE Collections SET
                Name = @Name,
                ColorHex = @ColorHex,
                Icon = @Icon,
                SortOrder = @SortOrder
            WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, collection);
    }

    public async Task<int> DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "DELETE FROM Collections WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, new { Id = id });
    }
}

public class SettingsRepository : ISettingsRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SettingsRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT Value FROM Settings WHERE Key = 'AppSettings' LIMIT 1;";
        var json = await conn.QuerySingleOrDefaultAsync<string>(sql);
        if (string.IsNullOrWhiteSpace(json))
        {
            var defaultSettings = new AppSettings();
            await SaveSettingsAsync(defaultSettings);
            return defaultSettings;
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        using var conn = _factory.CreateConnection();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        const string sql = @"
            INSERT INTO Settings (Key, Value) VALUES ('AppSettings', @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = @Value;";
        await conn.ExecuteAsync(sql, new { Value = json });
    }
}
