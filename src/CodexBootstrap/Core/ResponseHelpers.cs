using System.Text.Json;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// Helper functions for creating consistent API responses with standardized error codes
/// </summary>
public static class ResponseHelpers
{
    /// <summary>
    /// Creates a success response with data
    /// </summary>
    public static object CreateSuccessResponse(object? data = null, string? message = null)
    {
        return new
        {
            success = true,
            message = message ?? "Operation completed successfully",
            data = data,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response with standardized error code
    /// </summary>
    public static object CreateErrorResponse(string error, string? code = null, object? details = null)
    {
        var errorCode = code ?? ErrorCodes.INTERNAL_ERROR;
        var errorDetails = ErrorCodes.GetErrorDetails(errorCode);
        
        return new
        {
            success = false,
            error = error,
            code = errorCode,
            httpStatusCode = errorDetails.HttpStatusCode,
            technicalMessage = errorDetails.TechnicalMessage,
            userMessage = errorDetails.UserMessage,
            details = details,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static object CreateValidationErrorResponse(string field, string message)
    {
        return CreateErrorResponse(
            $"Validation failed for field '{field}': {message}", 
            ErrorCodes.VALIDATION_ERROR, 
            new { field, message }
        );
    }

    /// <summary>
    /// Creates a not found error response
    /// </summary>
    public static object CreateNotFoundResponse(string resource, string id)
    {
        return CreateErrorResponse(
            $"{resource} with id '{id}' not found", 
            ErrorCodes.NOT_FOUND, 
            new { resource, id }
        );
    }

    /// <summary>
    /// Creates an authentication required error response
    /// </summary>
    public static object CreateAuthenticationRequiredResponse()
    {
        return CreateErrorResponse(
            "Authentication is required to access this resource",
            ErrorCodes.AUTHENTICATION_REQUIRED
        );
    }

    /// <summary>
    /// Creates an authorization denied error response
    /// </summary>
    public static object CreateAuthorizationDeniedResponse(string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? "You don't have permission to perform this action",
            ErrorCodes.AUTHORIZATION_DENIED
        );
    }

    /// <summary>
    /// Creates a service unavailable error response
    /// </summary>
    public static object CreateServiceUnavailableResponse(string service, string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? $"{service} is temporarily unavailable",
            ErrorCodes.SERVICE_UNAVAILABLE,
            new { service }
        );
    }

    /// <summary>
    /// Creates a rate limit exceeded error response
    /// </summary>
    public static object CreateRateLimitExceededResponse(int retryAfterSeconds = 60)
    {
        return CreateErrorResponse(
            "Rate limit exceeded. Please wait before trying again.",
            ErrorCodes.RATE_LIMIT_EXCEEDED,
            new { retryAfterSeconds }
        );
    }

    /// <summary>
    /// Creates a business rule violation error response
    /// </summary>
    public static object CreateBusinessRuleViolationResponse(string rule, string? details = null)
    {
        return CreateErrorResponse(
            $"Business rule violation: {rule}",
            ErrorCodes.BUSINESS_RULE_VIOLATION,
            new { rule, details }
        );
    }

    /// <summary>
    /// Creates a resource conflict error response
    /// </summary>
    public static object CreateResourceConflictResponse(string resource, string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? $"Conflict with {resource}",
            ErrorCodes.RESOURCE_CONFLICT,
            new { resource }
        );
    }

    /// <summary>
    /// Creates a dependency not met error response
    /// </summary>
    public static object CreateDependencyNotMetResponse(string dependency, string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? $"Required dependency '{dependency}' is not met",
            ErrorCodes.DEPENDENCY_NOT_MET,
            new { dependency }
        );
    }

    /// <summary>
    /// Creates an external service error response
    /// </summary>
    public static object CreateExternalServiceErrorResponse(string service, string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? $"External service '{service}' is unavailable",
            ErrorCodes.EXTERNAL_API_ERROR,
            new { service }
        );
    }

    /// <summary>
    /// Creates an AI processing error response
    /// </summary>
    public static object CreateAIProcessingErrorResponse(string operation, string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? $"AI processing failed for operation '{operation}'",
            ErrorCodes.AI_PROCESSING_ERROR,
            new { operation }
        );
    }

    /// <summary>
    /// Creates a portal connection error response
    /// </summary>
    public static object CreatePortalConnectionErrorResponse(string portal, string? reason = null)
    {
        return CreateErrorResponse(
            reason ?? $"Failed to connect to portal '{portal}'",
            ErrorCodes.PORTAL_CONNECTION_ERROR,
            new { portal }
        );
    }

    /// <summary>
    /// Creates a breath loop response
    /// </summary>
    public static object CreateBreathLoopResponse(string id, string operation, bool success, string? message = null, object? data = null)
    {
        return new
        {
            id = id,
            operation = operation,
            success = success,
            message = message ?? (success ? "Operation completed successfully" : "Operation failed"),
            data = data,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an expand response
    /// </summary>
    public static object CreateExpandResponse(string id, string phase, bool expanded, string? message = null)
    {
        return new
        {
            id = id,
            phase = phase,
            expanded = expanded,
            message = message ?? (expanded ? "Expansion completed successfully" : "Expansion failed"),
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a validate response
    /// </summary>
    public static object CreateValidateResponse(string id, bool valid, string message)
    {
        return new
        {
            id = id,
            valid = valid,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a contract response
    /// </summary>
    public static object CreateContractResponse(string id, string phase, bool contracted, string? message = null)
    {
        return new
        {
            id = id,
            phase = phase,
            contracted = contracted,
            message = message ?? (contracted ? "Contraction completed successfully" : "Contraction failed"),
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a concept create response
    /// </summary>
    public static object CreateConceptCreateResponse(bool success, string conceptId, string message)
    {
        return new
        {
            success = success,
            conceptId = conceptId,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a concept define response
    /// </summary>
    public static object CreateConceptDefineResponse(string conceptId, Dictionary<string, object> properties, string message)
    {
        return new
        {
            conceptId = conceptId,
            properties = properties,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a concept relate response
    /// </summary>
    public static object CreateConceptRelateResponse(bool success, string relationshipId, string relationshipType, double weight, string message)
    {
        return new
        {
            success = success,
            relationshipId = relationshipId,
            relationshipType = relationshipType,
            weight = weight,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a concept search response
    /// </summary>
    public static object CreateConceptSearchResponse(object[] concepts, int totalCount, string message)
    {
        return new
        {
            concepts = concepts,
            totalCount = totalCount,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a concept semantic response
    /// </summary>
    public static object CreateConceptSemanticResponse(string conceptId, object analysis, string message)
    {
        return new
        {
            conceptId = conceptId,
            analysis = analysis,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a user concept link response
    /// </summary>
    public static object CreateUserConceptLinkResponse(bool success, string relationshipId, string relationshipType, double weight, string message)
    {
        return new
        {
            success = success,
            relationshipId = relationshipId,
            relationshipType = relationshipType,
            weight = weight,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a user concept unlink response
    /// </summary>
    public static object CreateUserConceptUnlinkResponse(bool success, string relationshipId, string message)
    {
        return new
        {
            success = success,
            relationshipId = relationshipId,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a user concepts response
    /// </summary>
    public static object CreateUserConceptsResponse(string userId, object[] concepts, int totalCount, string message)
    {
        return new
        {
            userId = userId,
            concepts = concepts,
            totalCount = totalCount,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a concept users response
    /// </summary>
    public static object CreateConceptUsersResponse(string conceptId, object[] users, int totalCount, string message)
    {
        return new
        {
            conceptId = conceptId,
            users = users,
            totalCount = totalCount,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a user concept relationship response
    /// </summary>
    public static object CreateUserConceptRelationshipResponse(string userId, string conceptId, string relationshipType, double weight, DateTime createdAt, string status, string message)
    {
        return new
        {
            userId = userId,
            conceptId = conceptId,
            relationshipType = relationshipType,
            weight = weight,
            createdAt = createdAt,
            status = status,
            message = message,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a health status response
    /// </summary>
    public static object CreateHealthStatusResponse(string status, TimeSpan uptime, long requestCount, int nodeCount, int edgeCount, int moduleCount, string version)
    {
        return new
        {
            status = status,
            uptime = uptime,
            requestCount = requestCount,
            nodeCount = nodeCount,
            edgeCount = edgeCount,
            moduleCount = moduleCount,
            version = version,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a module info response
    /// </summary>
    public static object CreateModuleInfoResponse(string id, string name, string version, string description, string title)
    {
        return new
        {
            id = id,
            name = name,
            version = version,
            description = description,
            title = title,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a spec atoms response
    /// </summary>
    public static object CreateSpecAtomsResponse(string moduleId, DateTime exportedAt, Node[] nodes, Edge[] edges)
    {
        return new
        {
            moduleId = moduleId,
            exportedAt = exportedAt,
            nodes = nodes,
            edges = edges,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a core atoms response
    /// </summary>
    public static object CreateCoreAtomsResponse(object atoms)
    {
        return new
        {
            result = new
            {
                atoms = atoms,
                success = true,
                message = "Core atoms retrieved successfully"
            },
            id = 1,
            status = "ranToCompletion",
            isCanceled = false,
            isCompleted = true,
            isCompletedSuccessfully = true,
            creationOptions = "none",
            isFaulted = false
        };
    }

    /// <summary>
    /// Creates a core spec response
    /// </summary>
    public static object CreateCoreSpecResponse(object spec)
    {
        return new
        {
            result = new
            {
                spec = spec,
                success = true,
                message = "Core spec retrieved successfully"
            },
            id = 2,
            status = "ranToCompletion",
            isCanceled = false,
            isCompleted = true,
            isCompletedSuccessfully = true,
            creationOptions = "none",
            isFaulted = false
        };
    }

    /// <summary>
    /// Creates a generic API response wrapper
    /// </summary>
    public static object CreateApiResponse(object result, bool success = true, string? message = null, int id = 1)
    {
        return new
        {
            result = result,
            success = success,
            message = message ?? (success ? "Operation completed successfully" : "Operation failed"),
            id = id,
            status = "ranToCompletion",
            isCanceled = false,
            isCompleted = true,
            isCompletedSuccessfully = success,
            creationOptions = "none",
            isFaulted = !success,
            timestamp = DateTime.UtcNow
        };
    }
}

