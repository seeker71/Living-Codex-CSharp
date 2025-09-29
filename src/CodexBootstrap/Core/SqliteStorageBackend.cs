using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace CodexBootstrap.Core;

/// <summary>
/// SQLite-based storage backend
/// </summary>
public class SqliteStorageBackend : IStorageBackend
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ICodexLogger _logger;

    public SqliteStorageBackend(string connectionString = "Data Source=data/codex.db")
    {
        _connectionString = connectionString;
        _logger = new Log4NetLogger(typeof(SqliteStorageBackend));
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
        // Ensure data directory exists
        var dataDir = Path.GetDirectoryName(_connectionString.Replace("Data Source=", ""));
        if (!string.IsNullOrEmpty(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Create tables
        var createNodesTable = @"
            CREATE TABLE IF NOT EXISTS nodes (
                id TEXT PRIMARY KEY,
                type_id TEXT NOT NULL,
                state TEXT NOT NULL,
                locale TEXT,
                title TEXT,
                description TEXT,
                content TEXT,
                meta TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )";

        var createEdgesTable = @"
            CREATE TABLE IF NOT EXISTS edges (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                from_id TEXT NOT NULL,
                to_id TEXT NOT NULL,
                role TEXT NOT NULL,
                role_id TEXT,
                weight REAL,
                meta TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (from_id) REFERENCES nodes(id),
                FOREIGN KEY (to_id) REFERENCES nodes(id)
            )";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_nodes_type_id ON nodes(type_id);
            CREATE INDEX IF NOT EXISTS idx_nodes_state ON nodes(state);
            CREATE INDEX IF NOT EXISTS idx_edges_from_id ON edges(from_id);
            CREATE INDEX IF NOT EXISTS idx_edges_to_id ON edges(to_id);
            CREATE INDEX IF NOT EXISTS idx_edges_role ON edges(role);
            CREATE INDEX IF NOT EXISTS idx_edges_role_id ON edges(role_id);
        ";

        using var command1 = new SqliteCommand(createNodesTable, connection);
        await command1.ExecuteNonQueryAsync();

        using var command2 = new SqliteCommand(createEdgesTable, connection);
        await command2.ExecuteNonQueryAsync();

        using var command3 = new SqliteCommand(createIndexes, connection);
        await command3.ExecuteNonQueryAsync();
    }

    public async Task StoreNodeAsync(Node node)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT OR REPLACE INTO nodes (id, type_id, state, locale, title, description, content, meta, updated_at)
            VALUES (@id, @typeId, @state, @locale, @title, @description, @content, @meta, @updatedAt)";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", node.Id);
        command.Parameters.AddWithValue("@typeId", node.TypeId);
        command.Parameters.AddWithValue("@state", node.State.ToString());
        command.Parameters.AddWithValue("@locale", node.Locale ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@title", node.Title ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", node.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@content", node.Content != null ? JsonSerializer.Serialize(node.Content, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@meta", node.Meta != null ? JsonSerializer.Serialize(node.Meta, _jsonOptions) : (object)DBNull.Value);
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM nodes WHERE id = @id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return await MapNodeFromReader(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Node>> GetAllNodesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM nodes ORDER BY created_at";
        using var command = new SqliteCommand(sql, connection);
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

    public async Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM nodes WHERE type_id = @typeId ORDER BY created_at";
        using var command = new SqliteCommand(sql, connection);
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
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Disable foreign key enforcement to allow edge upserts independent of node order
        using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys=OFF;", connection))
        {
            await pragmaCmd.ExecuteNonQueryAsync();
        }

        var sql = @"
            INSERT INTO edges (from_id, to_id, role, role_id, weight, meta)
            VALUES (@fromId, @toId, @role, @roleId, @weight, @meta)";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@fromId", edge.FromId);
        command.Parameters.AddWithValue("@toId", edge.ToId);
        command.Parameters.AddWithValue("@role", edge.Role);
        command.Parameters.AddWithValue("@roleId", (object?)edge.RoleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@weight", edge.Weight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@meta", edge.Meta != null ? JsonSerializer.Serialize(edge.Meta, _jsonOptions) : (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM edges ORDER BY created_at";
        using var command = new SqliteCommand(sql, connection);
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
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM edges WHERE from_id = @fromId ORDER BY created_at";
        using var command = new SqliteCommand(sql, connection);
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
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "SELECT * FROM edges WHERE to_id = @toId ORDER BY created_at";
        using var command = new SqliteCommand(sql, connection);
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

    public async Task DeleteNodeAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Delete associated edges first
        var deleteEdgesSql = "DELETE FROM edges WHERE from_id = @id OR to_id = @id";
        using var deleteEdgesCommand = new SqliteCommand(deleteEdgesSql, connection);
        deleteEdgesCommand.Parameters.AddWithValue("@id", id);
        await deleteEdgesCommand.ExecuteNonQueryAsync();

        // Delete the node
        var deleteNodeSql = "DELETE FROM nodes WHERE id = @id";
        using var deleteNodeCommand = new SqliteCommand(deleteNodeSql, connection);
        deleteNodeCommand.Parameters.AddWithValue("@id", id);
        await deleteNodeCommand.ExecuteNonQueryAsync();
    }

    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM edges WHERE from_id = @fromId AND to_id = @toId AND role = @role";
        using var command = new SqliteCommand(sql, connection);
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

    public async Task<StorageStats> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var nodeCountSql = "SELECT COUNT(*) FROM nodes";
        using var nodeCountCommand = new SqliteCommand(nodeCountSql, connection);
        var nodeCount = Convert.ToInt32(await nodeCountCommand.ExecuteScalarAsync());

        var edgeCountSql = "SELECT COUNT(*) FROM edges";
        using var edgeCountCommand = new SqliteCommand(edgeCountSql, connection);
        var edgeCount = Convert.ToInt32(await edgeCountCommand.ExecuteScalarAsync());

        // Get database file size
        var dbPath = _connectionString.Replace("Data Source=", "");
        var totalSize = File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0;

        return new StorageStats(
            NodeCount: nodeCount,
            EdgeCount: edgeCount,
            TotalSizeBytes: totalSize,
            LastUpdated: DateTime.UtcNow
        );
    }

    private async Task<Node?> MapNodeFromReader(SqliteDataReader reader)
    {
        try
        {
            var id = reader.GetString(reader.GetOrdinal("id"));
            var typeId = reader.GetString(reader.GetOrdinal("type_id"));
            var stateStr = reader.GetString(reader.GetOrdinal("state"));
            var state = Enum.Parse<ContentState>(stateStr);
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

            return new Node(id, typeId, state, locale, title, description, content, meta);
        }
        catch (Exception ex)
        {
            // Log error and return null
            _logger.Error($"Error mapping node from reader: {ex.Message}", ex);
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
            var roleId = reader.IsDBNull(reader.GetOrdinal("role_id")) ? null : reader.GetString(reader.GetOrdinal("role_id"));
            double? weight = reader.IsDBNull(reader.GetOrdinal("weight")) ? null : reader.GetDouble(reader.GetOrdinal("weight"));
            
            Dictionary<string, object>? meta = null;
            if (!reader.IsDBNull(reader.GetOrdinal("meta")))
            {
                var metaJson = reader.GetString(reader.GetOrdinal("meta"));
                meta = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson, _jsonOptions);
            }

            return new Edge(fromId, toId, role, roleId, weight, meta);
        }
        catch (Exception ex)
        {
            // Log error and return null
            _logger.Error($"Error mapping edge from reader: {ex.Message}", ex);
            return null;
        }
    }
}
