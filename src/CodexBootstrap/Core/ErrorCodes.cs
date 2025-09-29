using System.Collections.Generic;

namespace CodexBootstrap.Core;

/// <summary>
/// Standardized error codes for consistent API error handling
/// </summary>
public static class ErrorCodes
{
    // Authentication & Authorization (1000-1999)
    public const string AUTHENTICATION_REQUIRED = "AUTH_REQUIRED";
    public const string AUTHENTICATION_FAILED = "AUTH_FAILED";
    public const string AUTHORIZATION_DENIED = "AUTH_DENIED";
    public const string INVALID_TOKEN = "INVALID_TOKEN";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
    public const string SESSION_INVALID = "SESSION_INVALID";
    public const string OAUTH_PROVIDER_ERROR = "OAUTH_ERROR";
    public const string OAUTH_CALLBACK_FAILED = "OAUTH_CALLBACK_FAILED";

    // Validation Errors (2000-2999)
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string REQUIRED_FIELD_MISSING = "REQUIRED_FIELD_MISSING";
    public const string INVALID_FORMAT = "INVALID_FORMAT";
    public const string INVALID_RANGE = "INVALID_RANGE";
    public const string INVALID_TYPE = "INVALID_TYPE";
    public const string DUPLICATE_VALUE = "DUPLICATE_VALUE";
    public const string INVALID_EMAIL = "INVALID_EMAIL";
    public const string INVALID_URL = "INVALID_URL";
    public const string INVALID_DATE = "INVALID_DATE";
    public const string INVALID_JSON = "INVALID_JSON";

    // Resource Errors (3000-3999)
    public const string NOT_FOUND = "NOT_FOUND";
    public const string ALREADY_EXISTS = "ALREADY_EXISTS";
    public const string RESOURCE_LOCKED = "RESOURCE_LOCKED";
    public const string RESOURCE_CONFLICT = "RESOURCE_CONFLICT";
    public const string RESOURCE_EXPIRED = "RESOURCE_EXPIRED";
    public const string RESOURCE_UNAVAILABLE = "RESOURCE_UNAVAILABLE";
    public const string NODE_NOT_FOUND = "NODE_NOT_FOUND";
    public const string EDGE_NOT_FOUND = "EDGE_NOT_FOUND";
    public const string MODULE_NOT_FOUND = "MODULE_NOT_FOUND";
    public const string CONCEPT_NOT_FOUND = "CONCEPT_NOT_FOUND";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";

    // Business Logic Errors (4000-4999)
    public const string BUSINESS_RULE_VIOLATION = "BUSINESS_RULE_VIOLATION";
    public const string INSUFFICIENT_PERMISSIONS = "INSUFFICIENT_PERMISSIONS";
    public const string OPERATION_NOT_ALLOWED = "OPERATION_NOT_ALLOWED";
    public const string QUOTA_EXCEEDED = "QUOTA_EXCEEDED";
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";
    public const string INVALID_STATE_TRANSITION = "INVALID_STATE_TRANSITION";
    public const string DEPENDENCY_NOT_MET = "DEPENDENCY_NOT_MET";
    public const string WORKFLOW_BLOCKED = "WORKFLOW_BLOCKED";

    // System Errors (5000-5999)
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string DATABASE_ERROR = "DATABASE_ERROR";
    public const string NETWORK_ERROR = "NETWORK_ERROR";
    public const string TIMEOUT_ERROR = "TIMEOUT_ERROR";
    public const string CONFIGURATION_ERROR = "CONFIGURATION_ERROR";
    public const string STORAGE_ERROR = "STORAGE_ERROR";
    public const string CACHE_ERROR = "CACHE_ERROR";
    public const string REGISTRY_ERROR = "REGISTRY_ERROR";
    public const string MODULE_LOAD_ERROR = "MODULE_LOAD_ERROR";

    // External Service Errors (6000-6999)
    public const string EXTERNAL_API_ERROR = "EXTERNAL_API_ERROR";
    public const string LLM_SERVICE_ERROR = "LLM_SERVICE_ERROR";
    public const string NEWS_SERVICE_ERROR = "NEWS_SERVICE_ERROR";
    public const string OAUTH_SERVICE_ERROR = "OAUTH_SERVICE_ERROR";
    public const string STORAGE_SERVICE_ERROR = "STORAGE_SERVICE_ERROR";
    public const string CACHE_SERVICE_ERROR = "CACHE_SERVICE_ERROR";
    public const string NOTIFICATION_SERVICE_ERROR = "NOTIFICATION_SERVICE_ERROR";

    // AI & Processing Errors (7000-7999)
    public const string AI_PROCESSING_ERROR = "AI_PROCESSING_ERROR";
    public const string CONCEPT_EXTRACTION_ERROR = "CONCEPT_EXTRACTION_ERROR";
    public const string RESONANCE_CALCULATION_ERROR = "RESONANCE_CALCULATION_ERROR";
    public const string TRANSLATION_ERROR = "TRANSLATION_ERROR";
    public const string IMAGE_PROCESSING_ERROR = "IMAGE_PROCESSING_ERROR";
    public const string CONTENT_EXTRACTION_ERROR = "CONTENT_EXTRACTION_ERROR";
    public const string PATTERN_ANALYSIS_ERROR = "PATTERN_ANALYSIS_ERROR";

    // Portal & External Integration Errors (8000-8999)
    public const string PORTAL_CONNECTION_ERROR = "PORTAL_CONNECTION_ERROR";
    public const string PORTAL_EXPLORATION_ERROR = "PORTAL_EXPLORATION_ERROR";
    public const string EXTERNAL_WORLD_ERROR = "EXTERNAL_WORLD_ERROR";
    public const string TEMPORAL_PORTAL_ERROR = "TEMPORAL_PORTAL_ERROR";
    public const string FRACTAL_EXPLORATION_ERROR = "FRACTAL_EXPLORATION_ERROR";

    /// <summary>
    /// Get error code details including HTTP status code and user-friendly message
    /// </summary>
    public static ErrorCodeDetails GetErrorDetails(string errorCode)
    {
        return ErrorCodeMap.TryGetValue(errorCode, out var details) 
            ? details 
            : new ErrorCodeDetails(500, "An unexpected error occurred", "Please try again or contact support if the problem persists.");
    }

    private static readonly Dictionary<string, ErrorCodeDetails> ErrorCodeMap = new()
    {
        // Authentication & Authorization
        [AUTHENTICATION_REQUIRED] = new(401, "Authentication required", "Please log in to access this resource."),
        [AUTHENTICATION_FAILED] = new(401, "Authentication failed", "Invalid credentials provided."),
        [AUTHORIZATION_DENIED] = new(403, "Access denied", "You don't have permission to perform this action."),
        [INVALID_TOKEN] = new(401, "Invalid token", "The provided token is invalid or malformed."),
        [TOKEN_EXPIRED] = new(401, "Token expired", "Your session has expired. Please log in again."),
        [SESSION_INVALID] = new(401, "Invalid session", "Your session is no longer valid. Please log in again."),
        [OAUTH_PROVIDER_ERROR] = new(502, "OAuth provider error", "There was an issue with the OAuth provider. Please try again."),
        [OAUTH_CALLBACK_FAILED] = new(400, "OAuth callback failed", "The OAuth callback could not be processed."),

        // Validation Errors
        [VALIDATION_ERROR] = new(400, "Validation error", "The request data is invalid. Please check your input."),
        [REQUIRED_FIELD_MISSING] = new(400, "Required field missing", "A required field is missing from your request."),
        [INVALID_FORMAT] = new(400, "Invalid format", "The data format is not valid for this field."),
        [INVALID_RANGE] = new(400, "Invalid range", "The value is outside the allowed range."),
        [INVALID_TYPE] = new(400, "Invalid type", "The data type is not valid for this field."),
        [DUPLICATE_VALUE] = new(409, "Duplicate value", "This value already exists and must be unique."),
        [INVALID_EMAIL] = new(400, "Invalid email", "Please provide a valid email address."),
        [INVALID_URL] = new(400, "Invalid URL", "Please provide a valid URL."),
        [INVALID_DATE] = new(400, "Invalid date", "Please provide a valid date."),
        [INVALID_JSON] = new(400, "Invalid JSON", "The request body contains invalid JSON."),

        // Resource Errors
        [NOT_FOUND] = new(404, "Resource not found", "The requested resource could not be found."),
        [ALREADY_EXISTS] = new(409, "Resource already exists", "A resource with this identifier already exists."),
        [RESOURCE_LOCKED] = new(423, "Resource locked", "The resource is currently locked and cannot be modified."),
        [RESOURCE_CONFLICT] = new(409, "Resource conflict", "There is a conflict with the current state of the resource."),
        [RESOURCE_EXPIRED] = new(410, "Resource expired", "The requested resource has expired."),
        [RESOURCE_UNAVAILABLE] = new(503, "Resource unavailable", "The requested resource is temporarily unavailable."),
        [NODE_NOT_FOUND] = new(404, "Node not found", "The requested node could not be found in the registry."),
        [EDGE_NOT_FOUND] = new(404, "Edge not found", "The requested edge could not be found in the registry."),
        [MODULE_NOT_FOUND] = new(404, "Module not found", "The requested module is not available."),
        [CONCEPT_NOT_FOUND] = new(404, "Concept not found", "The requested concept could not be found."),
        [USER_NOT_FOUND] = new(404, "User not found", "The requested user could not be found."),

        // Business Logic Errors
        [BUSINESS_RULE_VIOLATION] = new(422, "Business rule violation", "The operation violates a business rule."),
        [INSUFFICIENT_PERMISSIONS] = new(403, "Insufficient permissions", "You don't have sufficient permissions for this operation."),
        [OPERATION_NOT_ALLOWED] = new(405, "Operation not allowed", "This operation is not allowed in the current context."),
        [QUOTA_EXCEEDED] = new(429, "Quota exceeded", "You have exceeded your quota for this operation."),
        [RATE_LIMIT_EXCEEDED] = new(429, "Rate limit exceeded", "Too many requests. Please wait before trying again."),
        [INVALID_STATE_TRANSITION] = new(422, "Invalid state transition", "The requested state transition is not valid."),
        [DEPENDENCY_NOT_MET] = new(422, "Dependency not met", "Required dependencies are not satisfied."),
        [WORKFLOW_BLOCKED] = new(422, "Workflow blocked", "The workflow is blocked and cannot proceed."),

        // System Errors
        [INTERNAL_ERROR] = new(500, "Internal server error", "An unexpected error occurred. Please try again."),
        [SERVICE_UNAVAILABLE] = new(503, "Service unavailable", "The service is temporarily unavailable."),
        [DATABASE_ERROR] = new(500, "Database error", "A database error occurred. Please try again."),
        [NETWORK_ERROR] = new(502, "Network error", "A network error occurred. Please try again."),
        [TIMEOUT_ERROR] = new(504, "Request timeout", "The request timed out. Please try again."),
        [CONFIGURATION_ERROR] = new(500, "Configuration error", "A configuration error occurred. Please contact support."),
        [STORAGE_ERROR] = new(500, "Storage error", "A storage error occurred. Please try again."),
        [CACHE_ERROR] = new(500, "Cache error", "A cache error occurred. Please try again."),
        [REGISTRY_ERROR] = new(500, "Registry error", "A registry error occurred. Please try again."),
        [MODULE_LOAD_ERROR] = new(500, "Module load error", "A module failed to load. Please try again."),

        // External Service Errors
        [EXTERNAL_API_ERROR] = new(502, "External API error", "An external service error occurred. Please try again."),
        [LLM_SERVICE_ERROR] = new(502, "LLM service error", "The AI service is temporarily unavailable. Please try again."),
        [NEWS_SERVICE_ERROR] = new(502, "News service error", "The news service is temporarily unavailable. Please try again."),
        [OAUTH_SERVICE_ERROR] = new(502, "OAuth service error", "The OAuth service is temporarily unavailable. Please try again."),
        [STORAGE_SERVICE_ERROR] = new(502, "Storage service error", "The storage service is temporarily unavailable. Please try again."),
        [CACHE_SERVICE_ERROR] = new(502, "Cache service error", "The cache service is temporarily unavailable. Please try again."),
        [NOTIFICATION_SERVICE_ERROR] = new(502, "Notification service error", "The notification service is temporarily unavailable. Please try again."),

        // AI & Processing Errors
        [AI_PROCESSING_ERROR] = new(500, "AI processing error", "An error occurred during AI processing. Please try again."),
        [CONCEPT_EXTRACTION_ERROR] = new(500, "Concept extraction error", "Failed to extract concepts. Please try again."),
        [RESONANCE_CALCULATION_ERROR] = new(500, "Resonance calculation error", "Failed to calculate resonance. Please try again."),
        [TRANSLATION_ERROR] = new(500, "Translation error", "Failed to translate content. Please try again."),
        [IMAGE_PROCESSING_ERROR] = new(500, "Image processing error", "Failed to process image. Please try again."),
        [CONTENT_EXTRACTION_ERROR] = new(500, "Content extraction error", "Failed to extract content. Please try again."),
        [PATTERN_ANALYSIS_ERROR] = new(500, "Pattern analysis error", "Failed to analyze patterns. Please try again."),

        // Portal & External Integration Errors
        [PORTAL_CONNECTION_ERROR] = new(502, "Portal connection error", "Failed to connect to external portal. Please try again."),
        [PORTAL_EXPLORATION_ERROR] = new(500, "Portal exploration error", "Failed to explore external portal. Please try again."),
        [EXTERNAL_WORLD_ERROR] = new(502, "External world error", "Failed to connect to external world. Please try again."),
        [TEMPORAL_PORTAL_ERROR] = new(500, "Temporal portal error", "Failed to access temporal portal. Please try again."),
        [FRACTAL_EXPLORATION_ERROR] = new(500, "Fractal exploration error", "Failed to perform fractal exploration. Please try again."),
    };
}

/// <summary>
/// Error code details including HTTP status code and user messages
/// </summary>
public record ErrorCodeDetails(int HttpStatusCode, string TechnicalMessage, string UserMessage);
