using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public class GenericApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<GenericApiService> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IErrorHandlingService _errorHandlingService;

    public string BaseUrl { get; set; } = "http://localhost:5002";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public event EventHandler<ApiRequestEventArgs>? RequestStarted;
    public event EventHandler<ApiResponseEventArgs>? ResponseReceived;
    public event EventHandler<ApiErrorEventArgs>? ErrorOccurred;

    public GenericApiService(
        HttpClient httpClient, 
        ILogger<GenericApiService> logger,
        ILoggingService loggingService,
        IErrorHandlingService errorHandlingService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _loggingService = loggingService;
        _errorHandlingService = errorHandlingService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        _httpClient.Timeout = Timeout;
    }

    public async Task<TResponse> GetAsync<TResponse>(string route, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(route));
        return await SendAsync<TResponse>(request, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl(route))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json")
        };
        return await SendAsync<TResponse>(httpRequest, cancellationToken);
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, BuildUrl(route))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json")
        };
        return await SendAsync<TResponse>(httpRequest, cancellationToken);
    }

    public async Task<TResponse> DeleteAsync<TResponse>(string route, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, BuildUrl(route));
        return await SendAsync<TResponse>(request, cancellationToken);
    }

    public async Task<TResponse> PatchAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, BuildUrl(route))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json")
        };
        return await SendAsync<TResponse>(httpRequest, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = request.Method.Method;
        var route = request.RequestUri?.PathAndQuery ?? "unknown";

        try
        {
            // Log request start
            OnRequestStarted(new ApiRequestEventArgs
            {
                Route = route,
                Method = method,
                RequestBody = null, // Could extract from content if needed
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("API Request: {Method} {Route}", method, route);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            // Log response
            OnResponseReceived(new ApiResponseEventArgs
            {
                Route = route,
                Method = method,
                StatusCode = (int)response.StatusCode,
                Duration = stopwatch.Elapsed,
                ResponseBody = null,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("API Response: {Method} {Route} - {StatusCode} ({Duration}ms)", 
                method, route, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            OnErrorOccurred(new ApiErrorEventArgs
            {
                Route = route,
                Method = method,
                Exception = ex,
                StatusCode = null,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogError(ex, "API Error: {Method} {Route} - {Error}", method, route, ex.Message);
            _loggingService.LogError($"API Error: {method} {route}", ex, new { method, route, duration = stopwatch.ElapsedMilliseconds });
            _errorHandlingService.HandleError(ex, $"API Request: {method} {route}", new { method, route, duration = stopwatch.ElapsedMilliseconds });
            throw;
        }
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorMessage = $"API call failed with status {response.StatusCode}";
            
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, _jsonOptions);
                errorMessage = errorResponse?.Message ?? errorMessage;
            }
            catch
            {
                // If we can't parse the error response, use the raw content
                if (!string.IsNullOrEmpty(errorContent))
                {
                    errorMessage = errorContent;
                }
            }

            OnErrorOccurred(new ApiErrorEventArgs
            {
                Route = request.RequestUri?.PathAndQuery ?? "unknown",
                Method = request.Method.Method,
                Exception = new HttpRequestException(errorMessage),
                StatusCode = (int)response.StatusCode,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            });

            _errorHandlingService.HandleApiError(errorMessage, (int)response.StatusCode, $"API Response: {request.Method.Method} {request.RequestUri?.PathAndQuery}");
            throw new HttpRequestException(errorMessage, null, response.StatusCode);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(content))
        {
            // Handle empty responses - return default for value types, null for reference types
            if (typeof(TResponse).IsValueType)
            {
                return default(TResponse)!;
            }
            return default(TResponse)!;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions) ?? throw new InvalidOperationException("Deserialization returned null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response for {Method} {Route}: {Content}", 
                request.Method.Method, request.RequestUri?.PathAndQuery, content);
            throw new InvalidOperationException($"Failed to deserialize response: {ex.Message}", ex);
        }
    }

    private string BuildUrl(string route)
    {
        if (string.IsNullOrEmpty(route))
            return BaseUrl;

        if (route.StartsWith("http://") || route.StartsWith("https://"))
            return route;

        var baseUrl = BaseUrl.TrimEnd('/');
        var cleanRoute = route.TrimStart('/');
        return $"{baseUrl}/{cleanRoute}";
    }

    protected virtual void OnRequestStarted(ApiRequestEventArgs e)
    {
        RequestStarted?.Invoke(this, e);
    }

    protected virtual void OnResponseReceived(ApiResponseEventArgs e)
    {
        ResponseReceived?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(ApiErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(this, e);
    }
}
