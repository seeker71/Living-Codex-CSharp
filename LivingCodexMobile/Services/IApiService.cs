using System.Text.Json;

namespace LivingCodexMobile.Services;

public interface IApiService
{
    Task<TResponse> GetAsync<TResponse>(string route, CancellationToken cancellationToken = default);
    Task<TResponse> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> DeleteAsync<TResponse>(string route, CancellationToken cancellationToken = default);
    Task<TResponse> PatchAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default);
    
    // Raw methods for when you need more control
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    
    // Configuration
    string BaseUrl { get; set; }
    TimeSpan Timeout { get; set; }
    
    // Events for logging and error handling
    event EventHandler<ApiRequestEventArgs> RequestStarted;
    event EventHandler<ApiResponseEventArgs> ResponseReceived;
    event EventHandler<ApiErrorEventArgs> ErrorOccurred;
}

public class ApiRequestEventArgs : EventArgs
{
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public object? RequestBody { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiResponseEventArgs : EventArgs
{
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public object? ResponseBody { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiErrorEventArgs : EventArgs
{
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Exception Exception { get; set; } = null!;
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}