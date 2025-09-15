using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface IErrorHandlingService
{
    void HandleError(Exception exception, string? context = null, object? data = null);
    void HandleApiError(string message, int? statusCode = null, string? context = null);
    
    // Events for UI to display errors
    event EventHandler<ErrorInfo> ErrorOccurred;
    
    // Get errors for display
    List<ErrorInfo> GetErrors(int maxCount = 50);
    void ClearErrors();
}

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly List<ErrorInfo> _errors = new();
    private readonly object _lock = new();
    private const int MaxErrors = 500;

    public event EventHandler<ErrorInfo>? ErrorOccurred;

    public void HandleError(Exception exception, string? context = null, object? data = null)
    {
        var errorInfo = new ErrorInfo
        {
            Timestamp = DateTime.UtcNow,
            Message = exception.Message,
            Exception = exception.ToString(),
            Context = context,
            Data = data,
            Type = ErrorType.Exception
        };

        AddError(errorInfo);
    }

    public void HandleApiError(string message, int? statusCode = null, string? context = null)
    {
        var errorInfo = new ErrorInfo
        {
            Timestamp = DateTime.UtcNow,
            Message = message,
            StatusCode = statusCode,
            Context = context,
            Type = ErrorType.ApiError
        };

        AddError(errorInfo);
    }

    public List<ErrorInfo> GetErrors(int maxCount = 50)
    {
        lock (_lock)
        {
            return _errors.TakeLast(maxCount).ToList();
        }
    }

    public void ClearErrors()
    {
        lock (_lock)
        {
            _errors.Clear();
        }
    }

    private void AddError(ErrorInfo errorInfo)
    {
        lock (_lock)
        {
            _errors.Add(errorInfo);
            
            // Keep only the last MaxErrors entries
            if (_errors.Count > MaxErrors)
            {
                _errors.RemoveRange(0, _errors.Count - MaxErrors);
            }
        }

        ErrorOccurred?.Invoke(this, errorInfo);
    }
}

public class ErrorInfo
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public int? StatusCode { get; set; }
    public string? Context { get; set; }
    public object? Data { get; set; }
    public ErrorType Type { get; set; }
}

public enum ErrorType
{
    Exception,
    ApiError,
    ValidationError,
    NetworkError
}
