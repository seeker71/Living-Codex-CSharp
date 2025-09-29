using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

namespace CodexBootstrap.Core;

/// <summary>
/// Helper functions for request validation with standardized error responses
/// </summary>
public static class ValidationHelpers
{
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"^https?://[^\s/$.?#].[^\s]*$", RegexOptions.Compiled);
    private static readonly Regex NodeIdRegex = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Validates that a required field is not null or empty
    /// </summary>
    public static ValidationResult? ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }
        return null;
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    public static ValidationResult? ValidateEmail(string? email, string fieldName = "Email")
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }

        if (!EmailRegex.IsMatch(email))
        {
            return new ValidationResult($"{fieldName} must be a valid email address", new[] { fieldName });
        }

        return null;
    }

    /// <summary>
    /// Validates URL format
    /// </summary>
    public static ValidationResult? ValidateUrl(string? url, string fieldName = "Url")
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }

        if (!UrlRegex.IsMatch(url))
        {
            return new ValidationResult($"{fieldName} must be a valid URL", new[] { fieldName });
        }

        return null;
    }

    /// <summary>
    /// Validates node ID format
    /// </summary>
    public static ValidationResult? ValidateNodeId(string? nodeId, string fieldName = "NodeId")
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }

        if (!NodeIdRegex.IsMatch(nodeId))
        {
            return new ValidationResult($"{fieldName} must contain only alphanumeric characters, dots, underscores, and hyphens", new[] { fieldName });
        }

        return null;
    }

    /// <summary>
    /// Validates string length
    /// </summary>
    public static ValidationResult? ValidateLength(string? value, string fieldName, int minLength = 0, int maxLength = int.MaxValue)
    {
        if (value == null)
        {
            if (minLength > 0)
            {
                return new ValidationResult($"{fieldName} is required", new[] { fieldName });
            }
            return null;
        }

        if (value.Length < minLength)
        {
            return new ValidationResult($"{fieldName} must be at least {minLength} characters long", new[] { fieldName });
        }

        if (value.Length > maxLength)
        {
            return new ValidationResult($"{fieldName} must be no more than {maxLength} characters long", new[] { fieldName });
        }

        return null;
    }

    /// <summary>
    /// Validates numeric range
    /// </summary>
    public static ValidationResult? ValidateRange<T>(T value, string fieldName, T minValue, T maxValue) where T : IComparable<T>
    {
        if (value.CompareTo(minValue) < 0)
        {
            return new ValidationResult($"{fieldName} must be at least {minValue}", new[] { fieldName });
        }

        if (value.CompareTo(maxValue) > 0)
        {
            return new ValidationResult($"{fieldName} must be no more than {maxValue}", new[] { fieldName });
        }

        return null;
    }

    /// <summary>
    /// Validates that a value is not null
    /// </summary>
    public static ValidationResult? ValidateNotNull<T>(T? value, string fieldName) where T : class
    {
        if (value == null)
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }
        return null;
    }

    /// <summary>
    /// Validates that a collection is not empty
    /// </summary>
    public static ValidationResult? ValidateNotEmpty<T>(IEnumerable<T>? collection, string fieldName)
    {
        if (collection == null || !collection.Any())
        {
            return new ValidationResult($"{fieldName} must not be empty", new[] { fieldName });
        }
        return null;
    }

    /// <summary>
    /// Validates JSON format
    /// </summary>
    public static ValidationResult? ValidateJson(string? json, string fieldName = "Json")
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return new ValidationResult($"{fieldName} must be valid JSON", new[] { fieldName });
        }
    }

    /// <summary>
    /// Validates date format and range
    /// </summary>
    public static ValidationResult? ValidateDate(DateTime? date, string fieldName, DateTime? minDate = null, DateTime? maxDate = null)
    {
        if (date == null)
        {
            return new ValidationResult($"{fieldName} is required", new[] { fieldName });
        }

        if (minDate.HasValue && date < minDate.Value)
        {
            return new ValidationResult($"{fieldName} must be on or after {minDate.Value:yyyy-MM-dd}", new[] { fieldName });
        }

        if (maxDate.HasValue && date > maxDate.Value)
        {
            return new ValidationResult($"{fieldName} must be on or before {maxDate.Value:yyyy-MM-dd}", new[] { fieldName });
        }

        return null;
    }

    /// <summary>
    /// Validates that a value is one of the allowed values
    /// </summary>
    public static ValidationResult? ValidateEnum<T>(T value, string fieldName, T[] allowedValues) where T : struct
    {
        if (!allowedValues.Contains(value))
        {
            var allowedValuesString = string.Join(", ", allowedValues);
            return new ValidationResult($"{fieldName} must be one of: {allowedValuesString}", new[] { fieldName });
        }
        return null;
    }

    /// <summary>
    /// Validates multiple fields and returns the first error
    /// </summary>
    public static ValidationResult? ValidateMultiple(params ValidationResult?[] validations)
    {
        return validations.FirstOrDefault(v => v != null);
    }

    /// <summary>
    /// Validates all fields and returns all errors
    /// </summary>
    public static List<ValidationResult> ValidateAll(params ValidationResult?[] validations)
    {
        return validations.Where(v => v != null).Cast<ValidationResult>().ToList();
    }

    /// <summary>
    /// Creates a validation error response from validation results
    /// </summary>
    public static object CreateValidationErrorResponse(List<ValidationResult> validationResults)
    {
        var errors = validationResults.Select(vr => new
        {
            field = vr.MemberNames.FirstOrDefault() ?? "Unknown",
            message = vr.ErrorMessage
        }).ToList();

        return ResponseHelpers.CreateErrorResponse(
            "Validation failed",
            ErrorCodes.VALIDATION_ERROR,
            new { errors }
        );
    }

    /// <summary>
    /// Creates a validation error response from a single validation result
    /// </summary>
    public static object CreateValidationErrorResponse(ValidationResult validationResult)
    {
        return ResponseHelpers.CreateValidationErrorResponse(
            validationResult.MemberNames.FirstOrDefault() ?? "Unknown",
            validationResult.ErrorMessage ?? "Validation failed"
        );
    }
}

/// <summary>
/// Custom validation result with member names
/// </summary>
public class ValidationResultWithMembers
{
    public string ErrorMessage { get; }
    public IEnumerable<string> MemberNames { get; }

    public ValidationResultWithMembers(string errorMessage, IEnumerable<string> memberNames)
    {
        ErrorMessage = errorMessage;
        MemberNames = memberNames;
    }

    public ValidationResultWithMembers(string errorMessage, string memberName) : this(errorMessage, new[] { memberName })
    {
    }
}
