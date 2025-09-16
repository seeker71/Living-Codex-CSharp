using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;

namespace CodexBootstrap.Core.Storage;

/// <summary>
/// PostgreSQL-based Ice storage backend for high-performance, federated storage
/// Designed for massive scale and eventual federation across multiple data centers
/// </summary>
public class PostgreSqlIceStorageBackend : IIceStorageBackend
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ICodexLogger _logger;

    public PostgreSqlIceStorageBackend(string connectionString)
    {
        _connectionString = connectionString;
        _logger = new Log4NetLogger(typeof(PostgreSqlIceStorageBackend));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task InitializeAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Create tables with optimized schema for Ice nodes
        var createNodesTable = @"
            CREATE TABLE IF NOT EXISTS ice_nodes (
                id TEXT PRIMARY KEY,
                type_id TEXT NOT NULL,
                locale TEXT,
                title TEXT,
                description TEXT,
                content JSONB,
                meta JSONB,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            )";

        var createEdgesTable = @"
            CREATE TABLE IF NOT EXISTS ice_edges (
                id BIGSERIAL PRIMARY KEY,
                from_id TEXT NOT NULL,
                to_id TEXT NOT NULL,
                role TEXT NOT NULL,
                weight REAL,
                meta JSONB,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                UNIQUE(from_id, to_id, role)
            )";

        // Create optimized indexes for Ice nodes
        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_ice_nodes_type_id ON ice_nodes(type_id);
            CREATE INDEX IF NOT EXISTS idx_ice_nodes_created_at ON ice_nodes(created_at);
            CREATE INDEX IF NOT EXISTS idx_ice_nodes_meta_gin ON ice_nodes USING GIN(meta);
            CREATE INDEX IF NOT EXISTS idx_ice_nodes_content_gin ON ice_nodes USING GIN(content);
            
            CREATE INDEX IF NOT EXISTS idx_ice_edges_from_id ON ice_edges(from_id);
            CREATE INDEX IF NOT EXISTS idx_ice_edges_to_id ON ice_edges(to_id);
            CREATE INDEX IF NOT EXISTS idx_ice_edges_role ON ice_edges(role);
            CREATE INDEX IF NOT EXISTS idx_ice_edges_meta_gin ON ice_edges USING GIN(meta);
        ";

        using var command1 = new NpgsqlCommand(createNodesTable, connection);
        await command1.ExecuteNonQueryAsync();

        using var command2 = new NpgsqlCommand(createEdgesTable, connection);
        await command2.ExecuteNonQueryAsync();

        using var command3 = new NpgsqlCommand(createIndexes, connection);
        await command3.ExecuteNonQueryAsync();

        _logger.Info("PostgreSQL Ice storage backend initialized");
    }

    public async Task StoreIceNodeAsync(Node node)
    {
        if (node.State != ContentState.Ice)
        {
            throw new ArgumentException("Only Ice nodes can be stored in Ice storage backend");
        }

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO ice_nodes (id, type_id, locale, title, description, content, meta, updated_at)
            VALUES (@id, @typeId, @locale, @title, @description, @content, @meta, @updatedAt)
            ON CONFLICT (id) DO UPDATE SET
                type_id = EXCLUDED.type_id,
                locale = EXCLUDED.locale,
                title = EXCLUDED.title,
                description = EXCLUDED.description,
                content = EXCLUDED.content,
                meta = EXCLUDED.meta,
                updated_at = EXCLUDED.updated_at";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", node.Id);
        command.Parameters.AddWithValue("@typeId", node.TypeId);
        command.Parameters.AddWithValue("@locale", node.Locale ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@title", node.Title ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", node.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@content", node.Content != null ? JsonSerializer.Serialize(node.Content, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@meta", node.Meta != null ? JsonSerializer.Serialize(node.Meta, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Node?> GetIceNodeAsync(string id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_nodes WHERE id = @id";
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return await MapNodeFromReader(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Node>> GetAllIceNodesAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_nodes ORDER BY created_at";
        using var command = new NpgsqlCommand(sql, connection);
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

    public async Task<IEnumerable<Node>> GetIceNodesByTypeAsync(string typeId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_nodes WHERE type_id = @typeId ORDER BY created_at";
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@typeId", typeId);
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

    public async Task StoreEdgeAsync(Edge edge)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO ice_edges (from_id, to_id, role, weight, meta)
            VALUES (@fromId, @toId, @role, @weight, @meta)
            ON CONFLICT (from_id, to_id, role) DO UPDATE SET
                weight = EXCLUDED.weight,
                meta = EXCLUDED.meta";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", edge.FromId);
        command.Parameters.AddWithValue("@toId", edge.ToId);
        command.Parameters.AddWithValue("@role", edge.Role);
        command.Parameters.AddWithValue("@weight", edge.Weight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@meta", edge.Meta != null ? JsonSerializer.Serialize(edge.Meta, _jsonOptions) : (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_edges ORDER BY created_at";
        using var command = new NpgsqlCommand(sql, connection);
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

    public async Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_edges WHERE from_id = @fromId ORDER BY created_at";
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", fromId);
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

    public async Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM ice_edges WHERE to_id = @toId ORDER BY created_at";
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@toId", toId);
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

    public async Task DeleteIceNodeAsync(string id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Delete associated edges first
        var deleteEdgesSql = "DELETE FROM ice_edges WHERE from_id = @id OR to_id = @id";
        using var deleteEdgesCommand = new NpgsqlCommand(deleteEdgesSql, connection);
        deleteEdgesCommand.Parameters.AddWithValue("@id", id);
        await deleteEdgesCommand.ExecuteNonQueryAsync();

        // Delete the node
        var deleteNodeSql = "DELETE FROM ice_nodes WHERE id = @id";
        using var deleteNodeCommand = new NpgsqlCommand(deleteNodeSql, connection);
        deleteNodeCommand.Parameters.AddWithValue("@id", id);
        await deleteNodeCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM ice_edges WHERE from_id = @fromId AND to_id = @toId AND role = @role";
        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", fromId);
        command.Parameters.AddWithValue("@toId", toId);
        command.Parameters.AddWithValue("@role", role);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
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
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var nodeCountSql = "SELECT COUNT(*) FROM ice_nodes";
        using var nodeCountCommand = new NpgsqlCommand(nodeCountSql, connection);
        var nodeCount = Convert.ToInt32(await nodeCountCommand.ExecuteScalarAsync());

        var edgeCountSql = "SELECT COUNT(*) FROM ice_edges";
        using var edgeCountCommand = new NpgsqlCommand(edgeCountSql, connection);
        var edgeCount = Convert.ToInt32(await edgeCountCommand.ExecuteScalarAsync());

        // Get database size
        var sizeSql = "SELECT pg_database_size(current_database())";
        using var sizeCommand = new NpgsqlCommand(sizeSql, connection);
        var totalSize = Convert.ToInt64(await sizeCommand.ExecuteScalarAsync());

        return new IceStorageStats(
            IceNodeCount: nodeCount,
            EdgeCount: edgeCount,
            TotalSizeBytes: totalSize,
            LastUpdated: DateTime.UtcNow,
            BackendType: "PostgreSQL",
            BackendStats: new Dictionary<string, object>
            {
                ["connection_string"] = _connectionString.Split(';')[0], // Hide password
                ["version"] = await GetPostgreSqlVersionAsync(connection)
            }
        );
    }

    public async Task BatchStoreIceNodesAsync(IEnumerable<Node> nodes)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var writer = await connection.BeginBinaryImportAsync(
            "COPY ice_nodes (id, type_id, locale, title, description, content, meta) FROM STDIN (FORMAT BINARY)");

        foreach (var node in nodes)
        {
            if (node.State != ContentState.Ice)
            {
                _logger.Warn($"Skipping non-Ice node {node.Id} in Ice storage batch");
                continue;
            }

            await writer.StartRowAsync();
            await writer.WriteAsync(node.Id);
            await writer.WriteAsync(node.TypeId);
            await writer.WriteAsync(node.Locale ?? (object)DBNull.Value);
            await writer.WriteAsync(node.Title ?? (object)DBNull.Value);
            await writer.WriteAsync(node.Description ?? (object)DBNull.Value);
            await writer.WriteAsync(node.Content != null ? JsonSerializer.Serialize(node.Content, _jsonOptions) : (object)DBNull.Value);
            await writer.WriteAsync(node.Meta != null ? JsonSerializer.Serialize(node.Meta, _jsonOptions) : (object)DBNull.Value);
        }

        await writer.CompleteAsync();
    }

    public async Task BatchStoreEdgesAsync(IEnumerable<Edge> edges)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var writer = await connection.BeginBinaryImportAsync(
            "COPY ice_edges (from_id, to_id, role, weight, meta) FROM STDIN (FORMAT BINARY)");

        foreach (var edge in edges)
        {
            await writer.StartRowAsync();
            await writer.WriteAsync(edge.FromId);
            await writer.WriteAsync(edge.ToId);
            await writer.WriteAsync(edge.Role);
            await writer.WriteAsync(edge.Weight ?? (object)DBNull.Value);
            await writer.WriteAsync(edge.Meta != null ? JsonSerializer.Serialize(edge.Meta, _jsonOptions) : (object)DBNull.Value);
        }

        await writer.CompleteAsync();
    }

    public async Task<IEnumerable<Node>> SearchIceNodesAsync(string query, int limit = 100)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM ice_nodes 
            WHERE 
                title ILIKE @query OR 
                description ILIKE @query OR 
                content::text ILIKE @query OR 
                meta::text ILIKE @query
            ORDER BY created_at DESC 
            LIMIT @limit";

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@query", $"%{query}%");
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

    public async Task<IEnumerable<Node>> GetIceNodesByMetaAsync(string key, object value, int limit = 100)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT * FROM ice_nodes 
            WHERE meta @> @query
            ORDER BY created_at DESC 
            LIMIT @limit";

        using var command = new NpgsqlCommand(sql, connection);
        var queryJson = JsonSerializer.Serialize(new Dictionary<string, object> { [key] = value }, _jsonOptions);
        command.Parameters.AddWithValue("@query", queryJson);
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

    private async Task<Node?> MapNodeFromReader(NpgsqlDataReader reader)
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

            return new Node(id, typeId, ContentState.Ice, locale, title, description, content, meta);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error mapping Ice node from reader: {ex.Message}", ex);
            return null;
        }
    }

    private Edge? MapEdgeFromReader(NpgsqlDataReader reader)
    {
        try
        {
            var fromId = reader.GetString(reader.GetOrdinal("from_id"));
            var toId = reader.GetString(reader.GetOrdinal("to_id"));
            var role = reader.GetString(reader.GetOrdinal("role"));
            double? weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? null : reader.GetDouble(reader.GetOrdinal("weight"));
            
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
            _logger.Error($"Error mapping edge from reader: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<string> GetPostgreSqlVersionAsync(NpgsqlConnection connection)
    {
        try
        {
            using var command = new NpgsqlCommand("SELECT version()", connection);
            var version = await command.ExecuteScalarAsync();
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}
