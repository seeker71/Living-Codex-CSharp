using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace CodexBootstrap.Core.Storage;

/// <summary>
/// SQLite-based Water storage backend for semi-persistent, local caching
/// Optimized for Water nodes (cache, generated code, derived data)
/// </summary>
public class SqliteWaterStorageBackend : IWaterStorageBackend
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ICodexLogger _logger;
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(30);

    public SqliteWaterStorageBackend(string connectionString = "Data Source=data/water_cache.db")
    {
        _connectionString = connectionString;
        _logger = new Log4NetLogger(typeof(SqliteWaterStorageBackend));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task InitializeAsync()
    {
        // Ensure data directory exists
        var dataDir = Path.GetDirectoryName(_connectionString.Replace("Data Source=", ""));
        if (!string.IsNullOrEmpty(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Create tables optimized for Water nodes with expiry
        var createWaterNodesTable = @"
            CREATE TABLE IF NOT EXISTS water_nodes (
                id TEXT PRIMARY KEY,
                type_id TEXT NOT NULL,
                locale TEXT,
                title TEXT,
                description TEXT,
                content TEXT,
                meta TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                expires_at DATETIME NOT NULL,
                generated_from TEXT
            )";

        // Create table for Water edges with expiry
        var createWaterEdgesTable = @"
            CREATE TABLE IF NOT EXISTS water_edges (
                from_id TEXT NOT NULL,
                to_id TEXT NOT NULL,
                role TEXT NOT NULL,
                weight REAL,
                meta TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                expires_at DATETIME NOT NULL,
                PRIMARY KEY (from_id, to_id, role)
            )";

        // Create indexes for Water nodes and edges
        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_water_nodes_type_id ON water_nodes(type_id);
            CREATE INDEX IF NOT EXISTS idx_water_nodes_expires_at ON water_nodes(expires_at);
            CREATE INDEX IF NOT EXISTS idx_water_nodes_generated_from ON water_nodes(generated_from);
            CREATE INDEX IF NOT EXISTS idx_water_nodes_created_at ON water_nodes(created_at);
            CREATE INDEX IF NOT EXISTS idx_water_edges_from_id ON water_edges(from_id);
            CREATE INDEX IF NOT EXISTS idx_water_edges_to_id ON water_edges(to_id);
            CREATE INDEX IF NOT EXISTS idx_water_edges_expires_at ON water_edges(expires_at);
        ";

        using var command1 = new SqliteCommand(createWaterNodesTable, connection);
        await command1.ExecuteNonQueryAsync();

        using var command2 = new SqliteCommand(createWaterEdgesTable, connection);
        await command2.ExecuteNonQueryAsync();

        using var command3 = new SqliteCommand(createIndexes, connection);
        await command3.ExecuteNonQueryAsync();

        _logger.Info("SQLite Water storage backend initialized");
    }

    public async Task StoreWaterNodeAsync(Node node, TimeSpan? expiry = null)
    {
        if (node.State != ContentState.Water)
        {
            throw new ArgumentException("Only Water nodes can be stored in Water storage backend");
        }

        var actualExpiry = expiry ?? _defaultExpiry;
        var expiresAt = DateTime.UtcNow.Add(actualExpiry);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT OR REPLACE INTO water_nodes (id, type_id, locale, title, description, content, meta, updated_at, expires_at, generated_from)
            VALUES (@id, @typeId, @locale, @title, @description, @content, @meta, @updatedAt, @expiresAt, @generatedFrom)";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", node.Id);
        command.Parameters.AddWithValue("@typeId", node.TypeId);
        command.Parameters.AddWithValue("@locale", node.Locale ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@title", node.Title ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", node.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@content", node.Content != null ? JsonSerializer.Serialize(node.Content, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@meta", node.Meta != null ? JsonSerializer.Serialize(node.Meta, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@expiresAt", expiresAt);
        
        // Extract generated_from from meta if available
        var generatedFrom = node.Meta?.ContainsKey("generatedFrom") == true ? 
            node.Meta["generatedFrom"]?.ToString() : null;
        command.Parameters.AddWithValue("@generatedFrom", generatedFrom ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Node?> GetWaterNodeAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_nodes 
            WHERE id = @id AND expires_at > @now";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return await MapNodeFromReader(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Node>> GetAllWaterNodesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_nodes 
            WHERE expires_at > @now 
            ORDER BY created_at";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        using var reader = await command.ExecuteReaderAsync();

        var nodes = new List<Node>();
        while (await reader.ReadAsync())
        {
            var node = await MapNodeFromReader(reader);
            if (node != null)
            {
                nodes.Add(node);
            }
        }

        return nodes;
    }

    public async Task<IEnumerable<Node>> GetWaterNodesByTypeAsync(string typeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_nodes 
            WHERE type_id = @typeId AND expires_at > @now 
            ORDER BY created_at";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@typeId", typeId);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        using var reader = await command.ExecuteReaderAsync();

        var nodes = new List<Node>();
        while (await reader.ReadAsync())
        {
            var node = await MapNodeFromReader(reader);
            if (node != null)
            {
                nodes.Add(node);
            }
        }

        return nodes;
    }

    public async Task DeleteWaterNodeAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM water_nodes WHERE id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task CleanupExpiredNodesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM water_nodes WHERE expires_at <= @now";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);

        var deletedCount = await command.ExecuteNonQueryAsync();
        
        if (deletedCount > 0)
        {
            _logger.Debug($"Cleaned up {deletedCount} expired Water nodes");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<WaterStorageStats> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var waterNodeCountSql = "SELECT COUNT(*) FROM water_nodes WHERE expires_at > @now";
        using var waterNodeCountCommand = new SqliteCommand(waterNodeCountSql, connection);
        waterNodeCountCommand.Parameters.AddWithValue("@now", DateTime.UtcNow);
        var waterNodeCount = Convert.ToInt32(await waterNodeCountCommand.ExecuteScalarAsync());

        var expiredNodeCountSql = "SELECT COUNT(*) FROM water_nodes WHERE expires_at <= @now";
        using var expiredNodeCountCommand = new SqliteCommand(expiredNodeCountSql, connection);
        expiredNodeCountCommand.Parameters.AddWithValue("@now", DateTime.UtcNow);
        var expiredNodeCount = Convert.ToInt32(await expiredNodeCountCommand.ExecuteScalarAsync());

        // Get average expiry time
        var avgExpirySql = @"
            SELECT AVG(julianday(expires_at) - julianday(created_at)) 
            FROM water_nodes 
            WHERE expires_at > @now";
        using var avgExpiryCommand = new SqliteCommand(avgExpirySql, connection);
        avgExpiryCommand.Parameters.AddWithValue("@now", DateTime.UtcNow);
        var avgExpiryDays = await avgExpiryCommand.ExecuteScalarAsync();
        var avgExpiry = avgExpiryDays != DBNull.Value ? 
            TimeSpan.FromDays(Convert.ToDouble(avgExpiryDays)) : TimeSpan.Zero;

        // Get database file size
        var dbPath = _connectionString.Replace("Data Source=", "");
        var totalSize = File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0;

        return new WaterStorageStats(
            WaterNodeCount: waterNodeCount,
            ExpiredNodeCount: expiredNodeCount,
            TotalSizeBytes: totalSize,
            LastUpdated: DateTime.UtcNow,
            AverageExpiry: avgExpiry,
            BackendStats: new Dictionary<string, object>
            {
                ["connection_string"] = _connectionString,
                ["default_expiry_minutes"] = _defaultExpiry.TotalMinutes
            }
        );
    }

    public async Task BatchStoreWaterNodesAsync(IEnumerable<Node> nodes, TimeSpan? expiry = null)
    {
        var actualExpiry = expiry ?? _defaultExpiry;
        var expiresAt = DateTime.UtcNow.Add(actualExpiry);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            var sql = @"
                INSERT OR REPLACE INTO water_nodes (id, type_id, locale, title, description, content, meta, updated_at, expires_at, generated_from)
                VALUES (@id, @typeId, @locale, @title, @description, @content, @meta, @updatedAt, @expiresAt, @generatedFrom)";

            using var command = new SqliteCommand(sql, connection, transaction);
            
            foreach (var node in nodes)
            {
                if (node.State != ContentState.Water)
                {
                    _logger.Warn($"Skipping non-Water node {node.Id} in Water storage batch");
                    continue;
                }

                command.Parameters.Clear();
                command.Parameters.AddWithValue("@id", node.Id);
                command.Parameters.AddWithValue("@typeId", node.TypeId);
                command.Parameters.AddWithValue("@locale", node.Locale ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@title", node.Title ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@description", node.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@content", node.Content != null ? JsonSerializer.Serialize(node.Content, _jsonOptions) : (object)DBNull.Value);
                command.Parameters.AddWithValue("@meta", node.Meta != null ? JsonSerializer.Serialize(node.Meta, _jsonOptions) : (object)DBNull.Value);
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@expiresAt", expiresAt);
                
                var generatedFrom = node.Meta?.ContainsKey("generatedFrom") == true ? 
                    node.Meta["generatedFrom"]?.ToString() : null;
                command.Parameters.AddWithValue("@generatedFrom", generatedFrom ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Node>> SearchWaterNodesAsync(string query, int limit = 100)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_nodes 
            WHERE 
                (title LIKE @query OR 
                 description LIKE @query OR 
                 content LIKE @query OR 
                 meta LIKE @query) AND
                expires_at > @now
            ORDER BY created_at DESC 
            LIMIT @limit";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@query", $"%{query}%");
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        command.Parameters.AddWithValue("@limit", limit);
        using var reader = await command.ExecuteReaderAsync();

        var nodes = new List<Node>();
        while (await reader.ReadAsync())
        {
            var node = await MapNodeFromReader(reader);
            if (node != null)
            {
                nodes.Add(node);
            }
        }

        return nodes;
    }

    public async Task StoreWaterEdgeAsync(Edge edge, TimeSpan? expiry = null)
    {
        var actualExpiry = expiry ?? _defaultExpiry;
        var expiresAt = DateTime.UtcNow.Add(actualExpiry);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT OR REPLACE INTO water_edges (from_id, to_id, role, weight, meta, updated_at, expires_at)
            VALUES (@fromId, @toId, @role, @weight, @meta, @updatedAt, @expiresAt)";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", edge.FromId);
        command.Parameters.AddWithValue("@toId", edge.ToId);
        command.Parameters.AddWithValue("@role", edge.Role);
        command.Parameters.AddWithValue("@weight", edge.Weight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@meta", edge.Meta != null ? JsonSerializer.Serialize(edge.Meta, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@expiresAt", expiresAt);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Edge>> GetAllWaterEdgesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_edges 
            WHERE expires_at > @now 
            ORDER BY created_at";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        using var reader = await command.ExecuteReaderAsync();

        var edges = new List<Edge>();
        while (await reader.ReadAsync())
        {
            var edge = MapEdgeFromReader(reader);
            if (edge != null)
            {
                edges.Add(edge);
            }
        }

        return edges;
    }

    public async Task<IEnumerable<Edge>> GetWaterEdgesFromAsync(string fromId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_edges 
            WHERE from_id = @fromId AND expires_at > @now 
            ORDER BY created_at";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", fromId);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        using var reader = await command.ExecuteReaderAsync();

        var edges = new List<Edge>();
        while (await reader.ReadAsync())
        {
            var edge = MapEdgeFromReader(reader);
            if (edge != null)
            {
                edges.Add(edge);
            }
        }

        return edges;
    }

    public async Task<IEnumerable<Edge>> GetWaterEdgesToAsync(string toId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM water_edges 
            WHERE to_id = @toId AND expires_at > @now 
            ORDER BY created_at";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@toId", toId);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        using var reader = await command.ExecuteReaderAsync();

        var edges = new List<Edge>();
        while (await reader.ReadAsync())
        {
            var edge = MapEdgeFromReader(reader);
            if (edge != null)
            {
                edges.Add(edge);
            }
        }

        return edges;
    }

    public async Task DeleteWaterEdgeAsync(string fromId, string toId, string role)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM water_edges WHERE from_id = @fromId AND to_id = @toId AND role = @role";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", fromId);
        command.Parameters.AddWithValue("@toId", toId);
        command.Parameters.AddWithValue("@role", role);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<Node?> MapNodeFromReader(SqliteDataReader reader)
    {
        try
        {
            var id = reader.GetString(reader.GetOrdinal("id"));
            var typeId = reader.GetString(reader.GetOrdinal("type_id"));
            var locale = reader.IsDBNull(reader.GetOrdinal("locale")) ? null : reader.GetString(reader.GetOrdinal("locale"));
            var title = reader.IsDBNull(reader.GetOrdinal("title")) ? null : reader.GetString(reader.GetOrdinal("title"));
            var description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description"));
            
            ContentRef? content = null;
            if (!reader.IsDBNull(reader.GetOrdinal("content")))
            {
                var contentJson = reader.GetString(reader.GetOrdinal("content"));
                content = JsonSerializer.Deserialize<ContentRef>(contentJson, _jsonOptions);
            }

            Dictionary<string, object>? meta = null;
            if (!reader.IsDBNull(reader.GetOrdinal("meta")))
            {
                var metaJson = reader.GetString(reader.GetOrdinal("meta"));
                meta = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson, _jsonOptions);
            }

            return new Node(id, typeId, ContentState.Water, locale, title, description, content, meta);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error mapping Water node from reader: {ex.Message}", ex);
            return null;
        }
    }

    private Edge? MapEdgeFromReader(SqliteDataReader reader)
    {
        try
        {
            var fromId = reader.GetString(reader.GetOrdinal("from_id"));
            var toId = reader.GetString(reader.GetOrdinal("to_id"));
            var role = reader.GetString(reader.GetOrdinal("role"));
            var weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? null : (double?)reader.GetDouble(reader.GetOrdinal("weight"));

            Dictionary<string, object>? meta = null;
            if (!reader.IsDBNull(reader.GetOrdinal("meta")))
            {
                var metaJson = reader.GetString(reader.GetOrdinal("meta"));
                meta = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson, _jsonOptions);
            }

            return new Edge(fromId, toId, role, weight, meta);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error mapping Water edge from reader: {ex.Message}", ex);
            return null;
        }
    }
}
