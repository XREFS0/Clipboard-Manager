using Dapper;
using MASA.ClipboardManager.Core.Enums;
using MASA.ClipboardManager.Core.Interfaces;
using MASA.ClipboardManager.Core.Models;
using MASA.ClipboardManager.Infrastructure.Database;

namespace MASA.ClipboardManager.Infrastructure.Repositories;

public class ClipboardRepository : IClipboardRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ClipboardRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<ClipboardItem?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ClipboardItems WHERE Id = @Id;";
        return await conn.QuerySingleOrDefaultAsync<ClipboardItem>(sql, new { Id = id });
    }

    public async Task<ClipboardItem?> GetByHashAsync(string contentHash)
    {
        if (string.IsNullOrEmpty(contentHash)) return null;
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ClipboardItems WHERE ContentHash = @ContentHash LIMIT 1;";
        return await conn.QuerySingleOrDefaultAsync<ClipboardItem>(sql, new { ContentHash = contentHash });
    }

    public async Task<IEnumerable<ClipboardItem>> GetRecentAsync(int limit = 100, int offset = 0)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            SELECT * FROM ClipboardItems 
            ORDER BY IsPinned DESC, LastUsedAt DESC 
            LIMIT @Limit OFFSET @Offset;";
        return await conn.QueryAsync<ClipboardItem>(sql, new { Limit = limit, Offset = offset });
    }

    public async Task<IEnumerable<ClipboardItem>> GetFavoritesAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ClipboardItems WHERE IsFavorite = 1 ORDER BY LastUsedAt DESC;";
        return await conn.QueryAsync<ClipboardItem>(sql);
    }

    public async Task<IEnumerable<ClipboardItem>> GetPinnedAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ClipboardItems WHERE IsPinned = 1 ORDER BY LastUsedAt DESC;";
        return await conn.QueryAsync<ClipboardItem>(sql);
    }

    public async Task<IEnumerable<ClipboardItem>> GetByCollectionAsync(string collectionId)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT * FROM ClipboardItems WHERE CollectionId = @CollectionId ORDER BY LastUsedAt DESC;";
        return await conn.QueryAsync<ClipboardItem>(sql, new { CollectionId = collectionId });
    }

    public async Task<IEnumerable<ClipboardItem>> SearchAsync(
        string query, 
        ClipboardContentType? type = null, 
        bool? isFavorite = null, 
        string? collectionId = null, 
        int limit = 100)
    {
        using var conn = _factory.CreateConnection();
        var sql = @"
            SELECT * FROM ClipboardItems 
            WHERE 1=1 ";
        
        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit);

        if (!string.IsNullOrWhiteSpace(query))
        {
            sql += " AND (SearchableText LIKE @Query OR Preview LIKE @Query)";
            parameters.Add("Query", $"%{query}%");
        }

        if (type.HasValue)
        {
            sql += " AND ContentType = @ContentType";
            parameters.Add("ContentType", (int)type.Value);
        }

        if (isFavorite.HasValue && isFavorite.Value)
        {
            sql += " AND IsFavorite = 1";
        }

        if (!string.IsNullOrWhiteSpace(collectionId))
        {
            sql += " AND CollectionId = @CollectionId";
            parameters.Add("CollectionId", collectionId);
        }

        sql += " ORDER BY IsPinned DESC, LastUsedAt DESC LIMIT @Limit;";

        return await conn.QueryAsync<ClipboardItem>(sql, parameters);
    }

    public async Task<int> AddAsync(ClipboardItem item)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            INSERT INTO ClipboardItems (
                Id, ContentType, ContentFormat, PlainTextContent, EncryptedPayload,
                ContentHash, Preview, SearchableText, SizeInBytes, CreatedAt,
                LastUsedAt, IsFavorite, IsPinned, IsSensitive, SensitiveType,
                AutoDeleteAt, CollectionId, ImageStoragePath, SourceProcessName,
                FileCount, FileExtension
            ) VALUES (
                @Id, @ContentType, @ContentFormat, @PlainTextContent, @EncryptedPayload,
                @ContentHash, @Preview, @SearchableText, @SizeInBytes, @CreatedAt,
                @LastUsedAt, @IsFavorite, @IsPinned, @IsSensitive, @SensitiveType,
                @AutoDeleteAt, @CollectionId, @ImageStoragePath, @SourceProcessName,
                @FileCount, @FileExtension
            );";
        return await conn.ExecuteAsync(sql, item);
    }

    public async Task<int> UpdateAsync(ClipboardItem item)
    {
        using var conn = _factory.CreateConnection();
        const string sql = @"
            UPDATE ClipboardItems SET
                PlainTextContent = @PlainTextContent,
                EncryptedPayload = @EncryptedPayload,
                Preview = @Preview,
                SearchableText = @SearchableText,
                SizeInBytes = @SizeInBytes,
                LastUsedAt = @LastUsedAt,
                IsFavorite = @IsFavorite,
                IsPinned = @IsPinned,
                CollectionId = @CollectionId
            WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, item);
    }

    public async Task<int> UpdateLastUsedAsync(string id, DateTime lastUsedAt)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "UPDATE ClipboardItems SET LastUsedAt = @LastUsedAt WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, new { Id = id, LastUsedAt = lastUsedAt });
    }

    public async Task<int> ToggleFavoriteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "UPDATE ClipboardItems SET IsFavorite = CASE WHEN IsFavorite = 1 THEN 0 ELSE 1 END WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> TogglePinAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "UPDATE ClipboardItems SET IsPinned = CASE WHEN IsPinned = 1 THEN 0 ELSE 1 END WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> SetCollectionAsync(string id, string? collectionId)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "UPDATE ClipboardItems SET CollectionId = @CollectionId WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, new { Id = id, CollectionId = collectionId });
    }

    public async Task<int> DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        const string sql = "DELETE FROM ClipboardItems WHERE Id = @Id;";
        return await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> ClearAllAsync(bool keepFavorites = true)
    {
        using var conn = _factory.CreateConnection();
        var sql = keepFavorites
            ? "DELETE FROM ClipboardItems WHERE IsFavorite = 0 AND IsPinned = 0;"
            : "DELETE FROM ClipboardItems;";
        return await conn.ExecuteAsync(sql);
    }

    public async Task<int> CleanupOldItemsAsync(int maxItems, int retentionDays, bool keepFavorites = true)
    {
        using var conn = _factory.CreateConnection();
        int deleted = 0;

        // 1. Delete items older than retention days
        if (retentionDays > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var sqlDate = keepFavorites
                ? "DELETE FROM ClipboardItems WHERE CreatedAt < @Cutoff AND IsFavorite = 0 AND IsPinned = 0;"
                : "DELETE FROM ClipboardItems WHERE CreatedAt < @Cutoff;";
            deleted += await conn.ExecuteAsync(sqlDate, new { Cutoff = cutoff });
        }

        // 2. Enforce max items count limit (keeping pinned/favorites)
        if (maxItems > 0)
        {
            var sqlLimit = keepFavorites
                ? @"DELETE FROM ClipboardItems 
                   WHERE Id IN (
                       SELECT Id FROM ClipboardItems 
                       WHERE IsFavorite = 0 AND IsPinned = 0 
                       ORDER BY LastUsedAt DESC 
                       LIMIT -1 OFFSET @MaxItems
                   );"
                : @"DELETE FROM ClipboardItems 
                   WHERE Id IN (
                       SELECT Id FROM ClipboardItems 
                       ORDER BY LastUsedAt DESC 
                       LIMIT -1 OFFSET @MaxItems
                   );";
            deleted += await conn.ExecuteAsync(sqlLimit, new { MaxItems = maxItems });
        }

        return deleted;
    }

    public async Task<int> DeleteExpiredSensitiveItemsAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "DELETE FROM ClipboardItems WHERE AutoDeleteAt IS NOT NULL AND AutoDeleteAt <= @Now;";
        return await conn.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = _factory.CreateConnection();
        const string sql = "SELECT COUNT(*) FROM ClipboardItems;";
        return await conn.ExecuteScalarAsync<int>(sql);
    }
}
