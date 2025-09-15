using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// User models
public class User
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public DateTime LastActive { get; set; }
    public bool IsActive { get; set; }
    public List<string> Permissions { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}

public class UserAuthRequest
{
    public string Provider { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

// Contribution models
public class Contribution
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Energy { get; set; }
    public double Resonance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

// API event models
public class ApiRequestEventArgs : EventArgs
{
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? RequestBody { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiResponseEventArgs : EventArgs
{
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApiErrorEventArgs : EventArgs
{
    public string Route { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public int? StatusCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
