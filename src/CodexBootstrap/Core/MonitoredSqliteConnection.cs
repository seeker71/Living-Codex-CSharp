using System.Data;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace CodexBootstrap.Core;

/// <summary>
/// SQLite connection wrapper with deadlock detection and monitoring
/// </summary>
public class MonitoredSqliteConnection : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseOperationMonitor _monitor;
    private readonly ICodexLogger _logger;
    private readonly string _connectionId;
    private bool _disposed = false;

    public MonitoredSqliteConnection(string connectionString, DatabaseOperationMonitor monitor, ICodexLogger logger)
    {
        _connection = new SqliteConnection(connectionString);
        _monitor = monitor;
        _logger = logger;
        _connectionId = Guid.NewGuid().ToString("N")[..8];
        
        // Configure SQLite for better concurrency
        _connection.DefaultTimeout = 30; // 30 second timeout
    }

    public async Task OpenAsync()
    {
        var context = _monitor.StartOperation("OpenConnection", "OPEN", _connection.ConnectionString);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _connection.OpenAsync();
            
            // Configure SQLite settings for better concurrency
            await ConfigureConnectionAsync();
            
            context.Complete(true);
            _logger.Debug($"SQLite connection {_connectionId} opened successfully");
        }
        catch (Exception ex)
        {
            context.Complete(false, ex);
            _logger.Error($"Failed to open SQLite connection {_connectionId}: {ex.Message}", ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.Warn($"Slow connection open: {_connectionId} took {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        var context = _monitor.StartOperation("ExecuteNonQuery", sql, _connection.ConnectionString);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var command = new SqliteCommand(sql, _connection);
            command.CommandTimeout = 30; // 30 second timeout
            
            var result = await command.ExecuteNonQueryAsync(cancellationToken);
            context.Complete(true);
            
            return result;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
        {
            _monitor.RecordLockWait("unknown", stopwatch.Elapsed);
            context.Complete(false, ex);
            _logger.Warn($"Database lock timeout on connection {_connectionId}: {ex.Message}");
            throw new DatabaseLockException($"Database is locked: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            context.Complete(false, ex);
            _logger.Error($"ExecuteNonQuery failed on connection {_connectionId}: {ex.Message}", ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.Warn($"Slow ExecuteNonQuery: {_connectionId} took {stopwatch.ElapsedMilliseconds}ms for: {sql[..Math.Min(100, sql.Length)]}");
            }
        }
    }

    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken cancellationToken = default)
    {
        var context = _monitor.StartOperation("ExecuteScalar", sql, _connection.ConnectionString);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var command = new SqliteCommand(sql, _connection);
            command.CommandTimeout = 30;
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            context.Complete(true);
            
            return result;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
        {
            _monitor.RecordLockWait("unknown", stopwatch.Elapsed);
            context.Complete(false, ex);
            _logger.Warn($"Database lock timeout on connection {_connectionId}: {ex.Message}");
            throw new DatabaseLockException($"Database is locked: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            context.Complete(false, ex);
            _logger.Error($"ExecuteScalar failed on connection {_connectionId}: {ex.Message}", ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.Warn($"Slow ExecuteScalar: {_connectionId} took {stopwatch.ElapsedMilliseconds}ms for: {sql[..Math.Min(100, sql.Length)]}");
            }
        }
    }

    public async Task<SqliteDataReader> ExecuteReaderAsync(string sql, CancellationToken cancellationToken = default)
    {
        var context = _monitor.StartOperation("ExecuteReader", sql, _connection.ConnectionString);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var command = new SqliteCommand(sql, _connection);
            command.CommandTimeout = 30;
            
            var reader = await command.ExecuteReaderAsync(cancellationToken);
            context.Complete(true);
            
            return reader;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5) // SQLITE_BUSY
        {
            _monitor.RecordLockWait("unknown", stopwatch.Elapsed);
            context.Complete(false, ex);
            _logger.Warn($"Database lock timeout on connection {_connectionId}: {ex.Message}");
            throw new DatabaseLockException($"Database is locked: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            context.Complete(false, ex);
            _logger.Error($"ExecuteReader failed on connection {_connectionId}: {ex.Message}", ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.Warn($"Slow ExecuteReader: {_connectionId} took {stopwatch.ElapsedMilliseconds}ms for: {sql[..Math.Min(100, sql.Length)]}");
            }
        }
    }

    public async Task<SqliteTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var context = _monitor.StartOperation("BeginTransaction", $"BEGIN TRANSACTION ({isolationLevel})", _connection.ConnectionString);
        
        try
        {
            var transaction = (SqliteTransaction)await _connection.BeginTransactionAsync(isolationLevel);
            context.Complete(true);
            
            _logger.Debug($"Transaction started on connection {_connectionId} with isolation level {isolationLevel}");
            return transaction;
        }
        catch (Exception ex)
        {
            context.Complete(false, ex);
            _logger.Error($"Failed to begin transaction on connection {_connectionId}: {ex.Message}", ex);
            throw;
        }
    }

    public SqliteCommand CreateCommand()
    {
        return _connection.CreateCommand();
    }

    private async Task ConfigureConnectionAsync()
    {
        // Configure SQLite for better concurrency and performance
        var pragmas = new[]
        {
            "PRAGMA journal_mode=WAL", // Write-Ahead Logging for better concurrency
            "PRAGMA synchronous=NORMAL", // Balance between safety and performance
            "PRAGMA cache_size=10000", // Increase cache size
            "PRAGMA temp_store=MEMORY", // Store temp tables in memory
            "PRAGMA mmap_size=268435456", // 256MB memory-mapped I/O
            "PRAGMA busy_timeout=30000", // 30 second busy timeout
            "PRAGMA foreign_keys=ON" // Enable foreign key constraints
        };

        foreach (var pragma in pragmas)
        {
            try
            {
                using var command = new SqliteCommand(pragma, _connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to set pragma '{pragma}' on connection {_connectionId}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _connection?.Dispose();
                _logger.Debug($"SQLite connection {_connectionId} disposed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error disposing SQLite connection {_connectionId}: {ex.Message}", ex);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Exception thrown when database operations timeout due to locks
/// </summary>
public class DatabaseLockException : Exception
{
    public DatabaseLockException(string message) : base(message) { }
    public DatabaseLockException(string message, Exception innerException) : base(message, innerException) { }
}
