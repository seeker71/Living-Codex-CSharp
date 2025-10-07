using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace CodexBootstrap.Core.Storage;

/// <summary>
/// SQLite-based Ice storage backend for development and testing
/// Provides the same interface as PostgreSQL but uses SQLite for simplicity
/// </summary>
public class SqliteIceStorageBackend : IIceStorageBackend
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ICodexLogger _logger;
    private readonly DatabaseOperationMonitor _monitor;

    public SqliteIceStorageBackend(string connectionString, DatabaseOperationMonitor? monitor = null)
    {
        _connectionString = connectionString;
        _logger = new Log4NetLogger(typeof(SqliteIceStorageBackend));
        _monitor = monitor ?? new DatabaseOperationMonitor(_logger);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
        };
    }

    public async Task InitializeAsync()
    {
        using var connection = new MonitoredSqliteConnection(_connectionString, _monitor, _logger);
        await connection.OpenAsync();

        // Create tables with optimized schema for Ice nodes
        var createNodesTable = @"
            CREATE TABLE IF NOT EXISTS ice_nodes (
                id TEXT PRIMARY KEY,
                type_id TEXT NOT NULL,
                locale TEXT,
                title TEXT,
                description TEXT,
                content TEXT,
                meta TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        var createEdgesTable = @"
            CREATE TABLE IF NOT EXISTS ice_edges (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                edge_id TEXT UNIQUE NOT NULL,
                from_id TEXT NOT NULL,
                to_id TEXT NOT NULL,
                role TEXT NOT NULL,
                weight REAL,
                properties TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_ice_nodes_type_id ON ice_nodes(type_id);
            CREATE INDEX IF NOT EXISTS idx_ice_nodes_created_at ON ice_nodes(created_at);
            CREATE INDEX IF NOT EXISTS idx_ice_edges_from_id ON ice_edges(from_id);
            CREATE INDEX IF NOT EXISTS idx_ice_edges_to_id ON ice_edges(to_id);
            CREATE INDEX IF NOT EXISTS idx_ice_edges_role ON ice_edges(role);
        ";

        using var command = connection.CreateCommand();
        command.CommandText = createNodesTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createEdgesTable;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createIndexes;
        await command.ExecuteNonQueryAsync();

        _logger.Info("SQLite Ice storage backend initialized");
    }

    public async Task StoreIceNodeAsync(Node node)
    {
        using var connection = new MonitoredSqliteConnection(_connectionString, _monitor, _logger);
        await connection.OpenAsync();

        var sql = @"
            INSERT OR REPLACE INTO ice_nodes (id, type_id, locale, title, description, content, meta, updated_at)
            VALUES (@id, @typeId, @locale, @title, @description, @content, @meta, CURRENT_TIMESTAMP)";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", node.Id);
        command.Parameters.AddWithValue("@typeId", node.TypeId);
        command.Parameters.AddWithValue("@locale", node.Locale ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@title", node.Title ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", node.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@content", JsonSerializer.Serialize(node.Content, _jsonOptions));
        command.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(node.Meta ?? new Dictionary<string, object>(), _jsonOptions));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Node?> GetIceNodeAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_nodes WHERE id = @id";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapNodeFromReader(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Node>> GetAllIceNodesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_nodes ORDER BY created_at DESC";
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        var nodes = new List<Node>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            nodes.Add(MapNodeFromReader(reader));
        }

        return nodes;
    }

    public async Task<IEnumerable<Node>> GetIceNodesByTypeAsync(string typeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_nodes WHERE type_id = @typeId ORDER BY created_at DESC";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@typeId", typeId);

        var nodes = new List<Node>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            nodes.Add(MapNodeFromReader(reader));
        }

        return nodes;
    }

    public async Task DeleteIceNodeAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM ice_nodes WHERE id = @id";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task StoreEdgeAsync(Edge edge)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT OR REPLACE INTO ice_edges (edge_id, from_id, to_id, role, weight, properties, updated_at)
            VALUES (@edgeId, @fromId, @toId, @role, @weight, @properties, CURRENT_TIMESTAMP)";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@edgeId", $"{edge.FromId}-{edge.ToId}-{edge.Role}");
        command.Parameters.AddWithValue("@fromId", edge.FromId);
        command.Parameters.AddWithValue("@toId", edge.ToId);
        command.Parameters.AddWithValue("@role", edge.Role);
        command.Parameters.AddWithValue("@weight", edge.Weight ?? 1.0);
        command.Parameters.AddWithValue("@properties", JsonSerializer.Serialize(edge.Meta ?? new Dictionary<string, object>(), _jsonOptions));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_edges ORDER BY created_at DESC";
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        var edges = new List<Edge>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            edges.Add(MapEdgeFromReader(reader));
        }

        return edges;
    }

    public async Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_edges WHERE from_id = @fromId ORDER BY created_at DESC";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@fromId", fromId);

        var edges = new List<Edge>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            edges.Add(MapEdgeFromReader(reader));
        }

        return edges;
    }

    public async Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_edges WHERE to_id = @toId ORDER BY created_at DESC";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@toId", toId);

        var edges = new List<Edge>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            edges.Add(MapEdgeFromReader(reader));
        }

        return edges;
    }

    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM ice_edges WHERE from_id = @fromId AND to_id = @toId AND role = @role";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@fromId", fromId);
        command.Parameters.AddWithValue("@toId", toId);
        command.Parameters.AddWithValue("@role", role);

        await command.ExecuteNonQueryAsync();
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

    public async Task<IceStorageStats> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var nodeCountSql = "SELECT COUNT(*) FROM ice_nodes";
        var edgeCountSql = "SELECT COUNT(*) FROM ice_edges";

        using var command = connection.CreateCommand();
        command.CommandText = nodeCountSql;
        var nodeCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        command.CommandText = edgeCountSql;
        var edgeCount = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new IceStorageStats(
            IceNodeCount: nodeCount,
            EdgeCount: edgeCount,
            TotalSizeBytes: 0, // SQLite doesn't easily provide size info
            LastUpdated: DateTime.UtcNow,
            BackendType: "SQLite",
            BackendStats: new Dictionary<string, object>
            {
                ["connection_string"] = _connectionString,
                ["is_memory"] = _connectionString.Contains(":memory:")
            }
        );
    }

    public async Task BatchStoreIceNodesAsync(IEnumerable<Node> nodes)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            var sql = @"
                INSERT OR REPLACE INTO ice_nodes (id, type_id, locale, title, description, content, meta, updated_at)
                VALUES (@id, @typeId, @locale, @title, @description, @content, @meta, CURRENT_TIMESTAMP)";

            foreach (var node in nodes)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@id", node.Id);
                command.Parameters.AddWithValue("@typeId", node.TypeId);
                command.Parameters.AddWithValue("@locale", node.Locale ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@title", node.Title ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@description", node.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@content", JsonSerializer.Serialize(node.Content, _jsonOptions));
                command.Parameters.AddWithValue("@meta", JsonSerializer.Serialize(node.Meta, _jsonOptions));

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

    public async Task BatchStoreEdgesAsync(IEnumerable<Edge> edges)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            var sql = @"
                INSERT OR REPLACE INTO ice_edges (edge_id, from_id, to_id, role, weight, properties, updated_at)
                VALUES (@edgeId, @fromId, @toId, @role, @weight, @properties, CURRENT_TIMESTAMP)";

            foreach (var edge in edges)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddWithValue("@edgeId", $"{edge.FromId}-{edge.ToId}-{edge.Role}");
                command.Parameters.AddWithValue("@fromId", edge.FromId);
                command.Parameters.AddWithValue("@toId", edge.ToId);
                command.Parameters.AddWithValue("@role", edge.Role);
                command.Parameters.AddWithValue("@weight", edge.Weight ?? 1.0);
                command.Parameters.AddWithValue("@properties", JsonSerializer.Serialize(edge.Meta ?? new Dictionary<string, object>(), _jsonOptions));

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

    public async Task<IEnumerable<Node>> SearchIceNodesAsync(string query, int limit = 100)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM ice_nodes 
            WHERE title LIKE @query OR description LIKE @query OR content LIKE @query
            ORDER BY created_at DESC 
            LIMIT @limit";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@query", $"%{query}%");
        command.Parameters.AddWithValue("@limit", limit);

        var nodes = new List<Node>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            nodes.Add(MapNodeFromReader(reader));
        }

        return nodes;
    }

    public async Task<IEnumerable<Node>> GetIceNodesByMetaAsync(string key, object value, int limit = 100)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM ice_nodes 
            WHERE json_extract(meta, '$.' || @key) = @value
            ORDER BY created_at DESC 
            LIMIT @limit";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value?.ToString() ?? "");
        command.Parameters.AddWithValue("@limit", limit);

        var nodes = new List<Node>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            nodes.Add(MapNodeFromReader(reader));
        }

        return nodes;
    }

    private Node MapNodeFromReader(SqliteDataReader reader)
    {
        var contentJson = reader.GetString(reader.GetOrdinal("content"));
        var metaJson = reader.GetString(reader.GetOrdinal("meta"));

        return new Node(
            Id: reader.GetString(reader.GetOrdinal("id")),
            TypeId: reader.GetString(reader.GetOrdinal("type_id")),
            State: ContentState.Ice,
            Locale: reader.IsDBNull(reader.GetOrdinal("locale")) ? null : reader.GetString(reader.GetOrdinal("locale")),
            Title: reader.IsDBNull(reader.GetOrdinal("title")) ? null : reader.GetString(reader.GetOrdinal("title")),
            Description: reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            Content: JsonSerializer.Deserialize<ContentRef>(contentJson, _jsonOptions),
            Meta: JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson, _jsonOptions)
        );
    }

    private Edge MapEdgeFromReader(SqliteDataReader reader)
    {
        var propertiesJson = reader.GetString(reader.GetOrdinal("properties"));
        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson, _jsonOptions) ?? new Dictionary<string, object>();

        return new Edge(
            FromId: reader.GetString(reader.GetOrdinal("from_id")),
            ToId: reader.GetString(reader.GetOrdinal("to_id")),
            Role: reader.GetString(reader.GetOrdinal("role")),
            Weight: reader.IsDBNull(reader.GetOrdinal("weight")) ? null : reader.GetDouble(reader.GetOrdinal("weight")),
            Meta: properties
        );
    }
}
