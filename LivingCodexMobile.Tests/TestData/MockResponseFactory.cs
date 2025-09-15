using LivingCodexMobile.Models;
using System.Text.Json;
using System.Net;
using System.Text;

namespace LivingCodexMobile.Tests.TestData;

/// <summary>
/// Factory class for creating mock HTTP responses
/// </summary>
public static class MockResponseFactory
{
    /// <summary>
    /// Creates a mock HTTP response with JSON content
    /// </summary>
    public static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with plain text content
    /// </summary>
    public static HttpResponseMessage CreateTextResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content, Encoding.UTF8, "text/plain")
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with HTML content
    /// </summary>
    public static HttpResponseMessage CreateHtmlResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content, Encoding.UTF8, "text/html")
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with XML content
    /// </summary>
    public static HttpResponseMessage CreateXmlResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content, Encoding.UTF8, "application/xml")
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with empty content
    /// </summary>
    public static HttpResponseMessage CreateEmptyResponse(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(string.Empty)
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with error content
    /// </summary>
    public static HttpResponseMessage CreateErrorResponse(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        var errorResponse = new ApiErrorResponse
        {
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        return CreateJsonResponse(errorResponse, statusCode);
    }

    /// <summary>
    /// Creates a mock HTTP response with not found content
    /// </summary>
    public static HttpResponseMessage CreateNotFoundResponse(string message = "Resource not found")
    {
        return CreateErrorResponse(message, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Creates a mock HTTP response with unauthorized content
    /// </summary>
    public static HttpResponseMessage CreateUnauthorizedResponse(string message = "Unauthorized")
    {
        return CreateErrorResponse(message, HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Creates a mock HTTP response with forbidden content
    /// </summary>
    public static HttpResponseMessage CreateForbiddenResponse(string message = "Forbidden")
    {
        return CreateErrorResponse(message, HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Creates a mock HTTP response with internal server error content
    /// </summary>
    public static HttpResponseMessage CreateInternalServerErrorResponse(string message = "Internal server error")
    {
        return CreateErrorResponse(message, HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Creates a mock HTTP response with timeout content
    /// </summary>
    public static HttpResponseMessage CreateTimeoutResponse(string message = "Request timeout")
    {
        return CreateErrorResponse(message, HttpStatusCode.RequestTimeout);
    }

    /// <summary>
    /// Creates a mock HTTP response with service unavailable content
    /// </summary>
    public static HttpResponseMessage CreateServiceUnavailableResponse(string message = "Service unavailable")
    {
        return CreateErrorResponse(message, HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Creates a mock HTTP response with bad gateway content
    /// </summary>
    public static HttpResponseMessage CreateBadGatewayResponse(string message = "Bad gateway")
    {
        return CreateErrorResponse(message, HttpStatusCode.BadGateway);
    }

    /// <summary>
    /// Creates a mock HTTP response with gateway timeout content
    /// </summary>
    public static HttpResponseMessage CreateGatewayTimeoutResponse(string message = "Gateway timeout")
    {
        return CreateErrorResponse(message, HttpStatusCode.GatewayTimeout);
    }

    /// <summary>
    /// Creates a mock HTTP response with too many requests content
    /// </summary>
    public static HttpResponseMessage CreateTooManyRequestsResponse(string message = "Too many requests")
    {
        return CreateErrorResponse(message, HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Creates a mock HTTP response with conflict content
    /// </summary>
    public static HttpResponseMessage CreateConflictResponse(string message = "Conflict")
    {
        return CreateErrorResponse(message, HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Creates a mock HTTP response with unprocessable entity content
    /// </summary>
    public static HttpResponseMessage CreateUnprocessableEntityResponse(string message = "Unprocessable entity")
    {
        return CreateErrorResponse(message, HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Creates a mock HTTP response with validation error content
    /// </summary>
    public static HttpResponseMessage CreateValidationErrorResponse(string message = "Validation error")
    {
        return CreateErrorResponse(message, HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Creates a mock HTTP response with rate limit exceeded content
    /// </summary>
    public static HttpResponseMessage CreateRateLimitExceededResponse(string message = "Rate limit exceeded")
    {
        return CreateErrorResponse(message, HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Creates a mock HTTP response with maintenance mode content
    /// </summary>
    public static HttpResponseMessage CreateMaintenanceModeResponse(string message = "Service is under maintenance")
    {
        return CreateErrorResponse(message, HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// Creates a mock HTTP response with custom headers
    /// </summary>
    public static HttpResponseMessage CreateResponseWithHeaders<T>(
        T data,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Dictionary<string, string>? headers = null)
    {
        var response = CreateJsonResponse(data, statusCode);
        
        if (headers != null)
        {
            foreach (var header in headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }
        }
        
        return response;
    }

    /// <summary>
    /// Creates a mock HTTP response with custom content type
    /// </summary>
    public static HttpResponseMessage CreateResponseWithContentType<T>(
        T data,
        string contentType,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(json, Encoding.UTF8, contentType)
        };
    }

    /// <summary>
    /// Creates a mock HTTP response with custom status code and message
    /// </summary>
    public static HttpResponseMessage CreateResponseWithStatusCodeAndMessage(
        string message,
        HttpStatusCode statusCode)
    {
        var response = new ApiResponse<object>
        {
            Success = statusCode == HttpStatusCode.OK,
            Data = null,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        return CreateJsonResponse(response, statusCode);
    }
}