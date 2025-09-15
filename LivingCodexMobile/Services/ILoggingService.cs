using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface ILoggingService
{
    void LogInfo(string message, object? data = null);
    void LogWarning(string message, object? data = null);
    void LogError(string message, Exception? exception = null, object? data = null);
    void LogDebug(string message, object? data = null);
    
    // Convenience methods for EnergyService
    void Info(string message, object? data = null);
    void Warn(string message, object? data = null);
    void Error(string message, Exception? exception = null, object? data = null);
    
    // Events for UI to display logs
    event EventHandler<LogEntry> LogEntryAdded;
    
    // Get logs for display
    List<LogEntry> GetLogs(int maxCount = 100);
    void ClearLogs();
}

public class LoggingService : ILoggingService
{
    private readonly List<LogEntry> _logs = new();
    private readonly object _lock = new();
    private const int MaxLogs = 1000;

    public event EventHandler<LogEntry>? LogEntryAdded;

    public void LogInfo(string message, object? data = null)
    {
        AddLog(LogLevel.Info, message, null, data);
    }

    public void LogWarning(string message, object? data = null)
    {
        AddLog(LogLevel.Warning, message, null, data);
    }

    public void LogError(string message, Exception? exception = null, object? data = null)
    {
        AddLog(LogLevel.Error, message, exception, data);
    }

    public void LogDebug(string message, object? data = null)
    {
        AddLog(LogLevel.Debug, message, null, data);
    }

    // Convenience methods for EnergyService
    public void Info(string message, object? data = null)
    {
        LogInfo(message, data);
    }

    public void Warn(string message, object? data = null)
    {
        LogWarning(message, data);
    }

    public void Error(string message, Exception? exception = null, object? data = null)
    {
        LogError(message, exception, data);
    }

    public List<LogEntry> GetLogs(int maxCount = 100)
    {
        lock (_lock)
        {
            return _logs.TakeLast(maxCount).ToList();
        }
    }

    public void ClearLogs()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }

    private void AddLog(LogLevel level, string message, Exception? exception, object? data)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Exception = exception?.ToString(),
            Data = data
        };

        lock (_lock)
        {
            _logs.Add(logEntry);
            
            // Keep only the last MaxLogs entries
            if (_logs.Count > MaxLogs)
            {
                _logs.RemoveRange(0, _logs.Count - MaxLogs);
            }
        }

        LogEntryAdded?.Invoke(this, logEntry);
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public object? Data { get; set; }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
