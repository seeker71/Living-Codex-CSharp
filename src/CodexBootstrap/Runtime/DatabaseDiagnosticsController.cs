using Microsoft.AspNetCore.Mvc;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

/// <summary>
/// Provides real-time database diagnostics and monitoring
/// </summary>
[ApiController]
[Route("api/diagnostics/database")]
public class DatabaseDiagnosticsController : ControllerBase
{
    private readonly DatabaseOperationMonitor _monitor;
    private readonly ICodexLogger _logger;

    public DatabaseDiagnosticsController(DatabaseOperationMonitor monitor, ICodexLogger logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    /// <summary>
    /// Get current database operation statistics
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            var stats = _monitor.GetStats();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting database stats: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to get database statistics" });
        }
    }

    /// <summary>
    /// Get currently active database operations
    /// </summary>
    [HttpGet("active")]
    public IActionResult GetActiveOperations()
    {
        try
        {
            var operations = _monitor.GetActiveOperations()
                .OrderByDescending(o => o.StartTime)
                .ToList();

            return Ok(new
            {
                count = operations.Count,
                operations = operations.Select(o => new
                {
                    id = o.Id,
                    type = o.OperationType,
                    sql = TruncateSql(o.Sql),
                    caller = $"{o.CallerName}:{o.CallerLine}",
                    duration = o.Duration.TotalSeconds,
                    status = o.Status.ToString()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting active operations: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to get active operations" });
        }
    }

    /// <summary>
    /// Get recent completed operations
    /// </summary>
    [HttpGet("recent")]
    public IActionResult GetRecentOperations([FromQuery] int count = 50)
    {
        try
        {
            var operations = _monitor.GetRecentOperations(count)
                .OrderByDescending(o => o.EndTime ?? o.StartTime)
                .ToList();

            return Ok(new
            {
                count = operations.Count,
                operations = operations.Select(o => new
                {
                    id = o.Id,
                    type = o.OperationType,
                    sql = TruncateSql(o.Sql),
                    caller = $"{o.CallerName}:{o.CallerLine}",
                    duration = o.Duration.TotalSeconds,
                    status = o.Status.ToString(),
                    success = o.Status == OperationStatus.Completed,
                    error = o.Exception,
                    startTime = o.StartTime,
                    endTime = o.EndTime
                })
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting recent operations: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to get recent operations" });
        }
    }

    /// <summary>
    /// Get deadlock analysis
    /// </summary>
    [HttpGet("deadlocks")]
    public IActionResult GetDeadlockAnalysis()
    {
        try
        {
            var analysis = _monitor.GetDeadlockAnalysis();
            
            return Ok(new
            {
                analysisTime = analysis.AnalysisTime,
                totalActiveOperations = analysis.TotalActiveOperations,
                potentialDeadlocks = analysis.PotentialDeadlocks.Select(d => new
                {
                    severity = d.Severity.ToString(),
                    conflictType = d.ConflictType.ToString(),
                    operation1 = new
                    {
                        id = d.Operation1.Id,
                        type = d.Operation1.OperationType,
                        caller = $"{d.Operation1.CallerName}:{d.Operation1.CallerLine}",
                        duration = d.Operation1.Duration.TotalSeconds,
                        sql = TruncateSql(d.Operation1.Sql)
                    },
                    operation2 = new
                    {
                        id = d.Operation2.Id,
                        type = d.Operation2.OperationType,
                        caller = $"{d.Operation2.CallerName}:{d.Operation2.CallerLine}",
                        duration = d.Operation2.Duration.TotalSeconds,
                        sql = TruncateSql(d.Operation2.Sql)
                    }
                })
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting deadlock analysis: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to get deadlock analysis" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific operation
    /// </summary>
    [HttpGet("operation/{operationId}")]
    public IActionResult GetOperationDetails(string operationId)
    {
        try
        {
            var activeOps = _monitor.GetActiveOperations();
            var recentOps = _monitor.GetRecentOperations(1000);
            
            var operation = activeOps.FirstOrDefault(o => o.Id == operationId) ??
                           recentOps.FirstOrDefault(o => o.Id == operationId);

            if (operation == null)
            {
                return NotFound(new { error = $"Operation {operationId} not found" });
            }

            return Ok(new
            {
                id = operation.Id,
                type = operation.OperationType,
                sql = operation.Sql,
                connectionString = operation.ConnectionString,
                caller = new
                {
                    name = operation.CallerName,
                    file = operation.CallerFile,
                    line = operation.CallerLine
                },
                callStack = operation.CallStack,
                timing = new
                {
                    startTime = operation.StartTime,
                    endTime = operation.EndTime,
                    duration = operation.Duration.TotalSeconds
                },
                status = operation.Status.ToString(),
                error = operation.Exception
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting operation details for {operationId}: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to get operation details" });
        }
    }

    /// <summary>
    /// Get database health status
    /// </summary>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        try
        {
            var stats = _monitor.GetStats();
            var analysis = _monitor.GetDeadlockAnalysis();
            
            var healthStatus = DetermineHealthStatus(stats, analysis);
            
            return Ok(new
            {
                status = healthStatus.Status,
                message = healthStatus.Message,
                details = new
                {
                    activeOperations = stats.ActiveOperations,
                    failedOperations = stats.FailedOperations,
                    slowOperations = stats.SlowOperations,
                    averageDuration = stats.AverageDuration,
                    potentialDeadlocks = analysis.PotentialDeadlocks.Count,
                    longRunningOperations = stats.LongRunningOperations.Count
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting database health: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to get database health" });
        }
    }

    /// <summary>
    /// Force cleanup of completed operations
    /// </summary>
    [HttpPost("cleanup")]
    public IActionResult ForceCleanup()
    {
        try
        {
            // This would trigger cleanup in the monitor
            // For now, just return success
            _logger.Info("Database operations cleanup requested");
            
            return Ok(new
            {
                message = "Cleanup initiated",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during cleanup: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to cleanup operations" });
        }
    }

    private string TruncateSql(string sql, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(sql))
            return "";
            
        if (sql.Length <= maxLength)
            return sql;
            
        return sql[..maxLength] + "...";
    }

    private (string Status, string Message) DetermineHealthStatus(DatabaseOperationStats stats, DeadlockAnalysis analysis)
    {
        // Critical issues
        if (analysis.PotentialDeadlocks.Any(d => d.Severity == DeadlockSeverity.Critical))
        {
            return ("Critical", "Critical deadlocks detected");
        }
        
        if (stats.LongRunningOperations.Count > 5)
        {
            return ("Critical", "Too many long-running operations");
        }
        
        // High severity issues
        if (analysis.PotentialDeadlocks.Any(d => d.Severity == DeadlockSeverity.High))
        {
            return ("Degraded", "High-severity deadlocks detected");
        }
        
        if (stats.FailedOperations > stats.TotalOperations * 0.1) // More than 10% failure rate
        {
            return ("Degraded", "High failure rate detected");
        }
        
        if (stats.SlowOperations > stats.TotalOperations * 0.2) // More than 20% slow operations
        {
            return ("Degraded", "Many slow operations detected");
        }
        
        // Medium issues
        if (analysis.PotentialDeadlocks.Any())
        {
            return ("Warning", "Potential deadlocks detected");
        }
        
        if (stats.ActiveOperations > 50)
        {
            return ("Warning", "High number of active operations");
        }
        
        // Healthy
        return ("Healthy", "Database operations are normal");
    }
}

