# Generic API Service

A configurable, generic API client for the Living Codex mobile app that provides one-liner API calls with centralized logging, error handling, and automatic serialization/deserialization.

## Features

- **Configurable Base URL**: Set the server URL via configuration or code
- **Generic Serialization**: Automatic JSON serialization/deserialization for any type
- **Centralized Logging**: All API calls are logged with timing and error information
- **Error Handling**: Centralized error handling with events for UI display
- **One-liner API Calls**: Simple method calls for GET, POST, PUT, DELETE, PATCH
- **Type Safety**: Full type safety with request/response type parameters
- **Event-driven**: Events for request start, response received, and errors

## Usage

### 1. Register Services

```csharp
// In MauiProgram.cs or your DI container setup
services.AddApiServices("http://localhost:5002");

// Or with configuration
services.AddApiServices(configuration);
```

### 2. Inject and Use

```csharp
public class MyService
{
    private readonly IApiService _apiService;

    public MyService(IApiService apiService)
    {
        _apiService = apiService;
    }

    // GET request
    public async Task<List<NewsItem>> GetNews(string userId)
    {
        var response = await _apiService.GetAsync<NewsResponse>($"/news/feed/{userId}");
        return response?.Items ?? new List<NewsItem>();
    }

    // POST request
    public async Task<Concept> CreateConcept(CreateConceptRequest request)
    {
        return await _apiService.PostAsync<CreateConceptRequest, Concept>("/concepts", request);
    }

    // PUT request
    public async Task<Concept> UpdateConcept(string id, UpdateConceptRequest request)
    {
        return await _apiService.PutAsync<UpdateConceptRequest, Concept>($"/concepts/{id}", request);
    }

    // DELETE request
    public async Task DeleteConcept(string id)
    {
        await _apiService.DeleteAsync<object>($"/concepts/{id}");
    }
}
```

### 3. Configuration

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5002",
    "Timeout": "00:00:30",
    "EnableLogging": true,
    "EnableRetry": true,
    "MaxRetryAttempts": 3
  }
}
```

### 4. Error Handling

```csharp
public class MyViewModel
{
    private readonly IErrorHandlingService _errorHandlingService;

    public MyViewModel(IErrorHandlingService errorHandlingService)
    {
        _errorHandlingService = errorHandlingService;
        
        // Subscribe to error events
        _errorHandlingService.ErrorOccurred += OnErrorOccurred;
    }

    private void OnErrorOccurred(object? sender, ErrorInfo error)
    {
        // Display error to user
        await DisplayAlert("Error", error.Message, "OK");
    }
}
```

### 5. Logging

```csharp
public class MyViewModel
{
    private readonly ILoggingService _loggingService;

    public MyViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
        
        // Subscribe to log events
        _loggingService.LogEntryAdded += OnLogEntryAdded;
    }

    private void OnLogEntryAdded(object? sender, LogEntry log)
    {
        // Display log in UI
        Console.WriteLine($"[{log.Level}] {log.Message}");
    }
}
```

## API Methods

### Generic Methods

- `GetAsync<TResponse>(string route, CancellationToken cancellationToken = default)`
- `PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default)`
- `PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default)`
- `DeleteAsync<TResponse>(string route, CancellationToken cancellationToken = default)`
- `PatchAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken cancellationToken = default)`

### Raw Methods

- `SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)`

## Events

### IApiService Events

- `RequestStarted`: Fired when an API request starts
- `ResponseReceived`: Fired when an API response is received
- `ErrorOccurred`: Fired when an API error occurs

### ILoggingService Events

- `LogEntryAdded`: Fired when a new log entry is added

### IErrorHandlingService Events

- `ErrorOccurred`: Fired when an error occurs

## Configuration Options

### IApiConfiguration

- `BaseUrl`: The base URL for API calls
- `Timeout`: Request timeout duration
- `EnableLogging`: Whether to enable detailed logging
- `EnableRetry`: Whether to enable automatic retry
- `MaxRetryAttempts`: Maximum number of retry attempts

## Error Handling

The service automatically handles:

- HTTP errors (4xx, 5xx status codes)
- Network errors (connection refused, timeout)
- JSON deserialization errors
- Timeout errors

All errors are logged and can be handled via the `IErrorHandlingService` events.

## Logging

All API calls are logged with:

- Request method and URL
- Response status code
- Response time
- Error details (if any)

Logs can be accessed via `ILoggingService.GetLogs()` and are limited to 1000 entries by default.

## Type Safety

The service uses generic type parameters to ensure type safety:

```csharp
// This will compile-time check that the response can be deserialized to NewsResponse
var response = await _apiService.GetAsync<NewsResponse>("/news/feed/123");

// This will compile-time check that the request can be serialized from CreateConceptRequest
var concept = await _apiService.PostAsync<CreateConceptRequest, Concept>("/concepts", request);
```

## Best Practices

1. **Use specific types**: Define specific request/response types instead of using `object`
2. **Handle errors gracefully**: Subscribe to error events and provide user feedback
3. **Use cancellation tokens**: Pass cancellation tokens for long-running operations
4. **Configure timeouts**: Set appropriate timeouts for your use case
5. **Monitor logs**: Use the logging service to monitor API performance and errors
