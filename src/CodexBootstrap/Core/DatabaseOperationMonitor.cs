using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodexBootstrap.Core;

/// <summary>
/// Monitors database operations for deadlocks, locks, and performance issues
/// Provides call stack tracing and real-time diagnostics
/// </summary>
public class DatabaseOperationMonitor : IDisposable
{
    private readonly ConcurrentDictionary<string, DatabaseOperation> _activeOperations = new();
    private readonly ConcurrentQueue<DatabaseOperation> _completedOperations = new();
    private readonly ConcurrentDictionary<string, int> _lockWaits = new();
    private readonly ICodexLogger _logger;
    private readonly Timer _cleanupTimer;
    private readonly Timer _deadlockCheckTimer;
    private bool _disposed = false;

    public DatabaseOperationMonitor(ICodexLogger logger)
    {
        _logger = logger;
        
        // Cleanup completed operations every 30 seconds
        _cleanupTimer = new Timer(CleanupCompletedOperations, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        // Check for deadlocks every 5 seconds
        _deadlockCheckTimer = new Timer(CheckForDeadlocks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Start monitoring a database operation
    /// </summary>
    public DatabaseOperationContext StartOperation(
        string operationType, 
        string sql, 
        string connectionString,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var callStack = GetCallStack();
        
        var operation = new DatabaseOperation
        {
            Id = operationId,
            OperationType = operationType,
            Sql = sql,
            ConnectionString = MaskConnectionString(connectionString),
            CallerName = callerName,
            CallerFile = callerFile,
            CallerLine = callerLine,
            CallStack = callStack,
            StartTime = DateTime.UtcNow,
            Status = OperationStatus.Running
        };

        _activeOperations.TryAdd(operationId, operation);
        
        _logger.Debug($"DB Operation Started: {operationId} - {operationType} in {callerName}");
        
        return new DatabaseOperationContext(this, operationId);
    }

    /// <summary>
    /// Complete a database operation
    /// </summary>
    public void CompleteOperation(string operationId, bool success, Exception? exception = null)
    {
        if (_activeOperations.TryRemove(operationId, out var operation))
        {
            operation.EndTime = DateTime.UtcNow;
            // Duration is calculated property, no need to set it
            operation.Status = success ? OperationStatus.Completed : OperationStatus.Failed;
            operation.Exception = exception?.Message;
            
            // Log slow operations
            if (operation.Duration > TimeSpan.FromSeconds(5))
            {
                _logger.Warn($"Slow DB Operation: {operationId} took {operation.Duration.TotalSeconds:F2}s - {operation.OperationType}");
            }
            
            // Log failed operations
            if (!success)
            {
                _logger.Error($"DB Operation Failed: {operationId} - {operation.OperationType} - {exception?.Message}", exception);
            }
            
            _completedOperations.Enqueue(operation);
        }
    }

    /// <summary>
    /// Record a lock wait event
    /// </summary>
    public void RecordLockWait(string tableName, TimeSpan waitTime)
    {
        _lockWaits.AddOrUpdate(tableName, 1, (key, value) => value + 1);
        
        if (waitTime > TimeSpan.FromSeconds(1))
        {
            _logger.Warn($"Database lock wait detected: {tableName} waited {waitTime.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Get current active operations
    /// </summary>
    public IEnumerable<DatabaseOperation> GetActiveOperations()
    {
        return _activeOperations.Values.ToList();
    }

    /// <summary>
    /// Get recent completed operations
    /// </summary>
    public IEnumerable<DatabaseOperation> GetRecentOperations(int count = 100)
    {
        return _completedOperations.TakeLast(count);
    }

    /// <summary>
    /// Get database operation statistics
    /// </summary>
    public DatabaseOperationStats GetStats()
    {
        var activeOps = _activeOperations.Values.ToList();
        var recentOps = _completedOperations.TakeLast(1000).ToList();
        
        return new DatabaseOperationStats
        {
            ActiveOperations = activeOps.Count,
            TotalOperations = _completedOperations.Count,
            FailedOperations = recentOps.Count(o => o.Status == OperationStatus.Failed),
            SlowOperations = recentOps.Count(o => o.Duration > TimeSpan.FromSeconds(5)),
            AverageDuration = recentOps.Any() ? recentOps.Average(o => o.Duration.TotalMilliseconds) : 0,
            LockWaits = _lockWaits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            LongRunningOperations = activeOps.Where(o => o.Duration > TimeSpan.FromSeconds(30)).ToList(),
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Get potential deadlock information
    /// </summary>
    public DeadlockAnalysis GetDeadlockAnalysis()
    {
        var activeOps = _activeOperations.Values.ToList();
        var potentialDeadlocks = new List<PotentialDeadlock>();
        
        // Analyze for circular wait conditions
        for (int i = 0; i < activeOps.Count; i++)
        {
            for (int j = i + 1; j < activeOps.Count; j++)
            {
                var op1 = activeOps[i];
                var op2 = activeOps[j];
                
                // Check if operations are accessing same tables and have been running long
                if (HasTableConflict(op1.Sql, op2.Sql) && 
                    op1.Duration > TimeSpan.FromSeconds(10) && 
                    op2.Duration > TimeSpan.FromSeconds(10))
                {
                    potentialDeadlocks.Add(new PotentialDeadlock
                    {
                        Operation1 = op1,
                        Operation2 = op2,
                        ConflictType = DetermineConflictType(op1.Sql, op2.Sql),
                        Severity = CalculateDeadlockSeverity(op1, op2)
                    });
                }
            }
        }
        
        return new DeadlockAnalysis
        {
            PotentialDeadlocks = potentialDeadlocks,
            AnalysisTime = DateTime.UtcNow,
            TotalActiveOperations = activeOps.Count
        };
    }

    private void CleanupCompletedOperations(object? state)
    {
        // Keep only the last 1000 completed operations
        while (_completedOperations.Count > 1000)
        {
            _completedOperations.TryDequeue(out _);
        }
    }

    private void CheckForDeadlocks(object? state)
    {
        var analysis = GetDeadlockAnalysis();
        
        if (analysis.PotentialDeadlocks.Any())
        {
            foreach (var deadlock in analysis.PotentialDeadlocks)
            {
                _logger.Error($"Potential deadlock detected: {deadlock.Operation1.Id} <-> {deadlock.Operation2.Id} " +
                             $"({deadlock.ConflictType}) - Severity: {deadlock.Severity}");
                
                _logger.Error($"Operation 1: {deadlock.Operation1.OperationType} in {deadlock.Operation1.CallerName} " +
                             $"running for {deadlock.Operation1.Duration.TotalSeconds:F1}s");
                _logger.Error($"Operation 2: {deadlock.Operation2.OperationType} in {deadlock.Operation2.CallerName} " +
                             $"running for {deadlock.Operation2.Duration.TotalSeconds:F1}s");
            }
        }
    }

    private string GetCallStack()
    {
        var stackTrace = new StackTrace(true);
        var frames = stackTrace.GetFrames()
            .Where(f => f.GetMethod()?.DeclaringType?.Namespace?.StartsWith("CodexBootstrap") == true)
            .Take(10)
            .Select(f => $"{f.GetMethod()?.DeclaringType?.Name}.{f.GetMethod()?.Name}:{f.GetFileLineNumber()}")
            .ToArray();
        
        return string.Join(" -> ", frames);
    }

    private string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Unknown";
            
        // Mask sensitive information
        return connectionString
            .Replace(Regex.Replace(connectionString, @"Password=([^;]+)", "Password=***"), "Password=***")
            .Replace(Regex.Replace(connectionString, @"User ID=([^;]+)", "User ID=***"), "User ID=***");
    }

    private bool HasTableConflict(string sql1, string sql2)
    {
        var tables1 = ExtractTables(sql1);
        var tables2 = ExtractTables(sql2);
        
        return tables1.Intersect(tables2, StringComparer.OrdinalIgnoreCase).Any();
    }

    private string[] ExtractTables(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return Array.Empty<string>();
            
        var tables = new List<string>();
        
        // Simple table extraction - could be enhanced
        var matches = Regex.Matches(sql, @"FROM\s+(\w+)|JOIN\s+(\w+)|UPDATE\s+(\w+)|INSERT\s+INTO\s+(\w+)", 
            RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (match.Groups[i].Success && !string.IsNullOrEmpty(match.Groups[i].Value))
                {
                    tables.Add(match.Groups[i].Value);
                }
            }
        }
        
        return tables.Distinct().ToArray();
    }

    private ConflictType DetermineConflictType(string sql1, string sql2)
    {
        var isWrite1 = Regex.IsMatch(sql1, @"INSERT|UPDATE|DELETE", RegexOptions.IgnoreCase);
        var isWrite2 = Regex.IsMatch(sql2, @"INSERT|UPDATE|DELETE", RegexOptions.IgnoreCase);
        
        if (isWrite1 && isWrite2)
            return ConflictType.WriteWrite;
        else if (isWrite1 || isWrite2)
            return ConflictType.ReadWrite;
        else
            return ConflictType.ReadRead;
    }

    private DeadlockSeverity CalculateDeadlockSeverity(DatabaseOperation op1, DatabaseOperation op2)
    {
        var maxDuration = Math.Max(op1.Duration.TotalSeconds, op2.Duration.TotalSeconds);
        
        return maxDuration switch
        {
            > 60 => DeadlockSeverity.Critical,
            > 30 => DeadlockSeverity.High,
            > 15 => DeadlockSeverity.Medium,
            _ => DeadlockSeverity.Low
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _deadlockCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Context for tracking a database operation
/// </summary>
public class DatabaseOperationContext : IDisposable
{
    private readonly DatabaseOperationMonitor _monitor;
    private readonly string _operationId;
    private bool _disposed = false;

    public DatabaseOperationContext(DatabaseOperationMonitor monitor, string operationId)
    {
        _monitor = monitor;
        _operationId = operationId;
    }

    public void Complete(bool success = true, Exception? exception = null)
    {
        if (!_disposed)
        {
            _monitor.CompleteOperation(_operationId, success, exception);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _monitor.CompleteOperation(_operationId, true);
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a database operation
/// </summary>
public class DatabaseOperation
{
    public string Id { get; set; } = "";
    public string OperationType { get; set; } = "";
    public string Sql { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public string CallerName { get; set; } = "";
    public string CallerFile { get; set; } = "";
    public int CallerLine { get; set; }
    public string CallStack { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
    public OperationStatus Status { get; set; }
    public string? Exception { get; set; }
}

/// <summary>
/// Database operation statistics
/// </summary>
public class DatabaseOperationStats
{
    public int ActiveOperations { get; set; }
    public int TotalOperations { get; set; }
    public int FailedOperations { get; set; }
    public int SlowOperations { get; set; }
    public double AverageDuration { get; set; }
    public Dictionary<string, int> LockWaits { get; set; } = new();
    public List<DatabaseOperation> LongRunningOperations { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Deadlock analysis results
/// </summary>
public class DeadlockAnalysis
{
    public List<PotentialDeadlock> PotentialDeadlocks { get; set; } = new();
    public DateTime AnalysisTime { get; set; }
    public int TotalActiveOperations { get; set; }
}

/// <summary>
/// Potential deadlock information
/// </summary>
public class PotentialDeadlock
{
    public DatabaseOperation Operation1 { get; set; } = new();
    public DatabaseOperation Operation2 { get; set; } = new();
    public ConflictType ConflictType { get; set; }
    public DeadlockSeverity Severity { get; set; }
}

public enum OperationStatus
{
    Running,
    Completed,
    Failed
}

public enum ConflictType
{
    ReadRead,
    ReadWrite,
    WriteWrite
}

public enum DeadlockSeverity
{
    Low,
    Medium,
    High,
    Critical
}
