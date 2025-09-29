using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core.Security
{
    /// <summary>
    /// Centralized input validation service for the Living Codex system
    /// </summary>
    public class InputValidator : IInputValidator
    {
        private readonly ICodexLogger _logger;
        private readonly Dictionary<string, Regex> _regexCache;

        // Common validation patterns
        private static readonly Regex EmailPattern = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        private static readonly Regex AlphanumericPattern = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
        private static readonly Regex SafeStringPattern = new(@"^[a-zA-Z0-9\s\-_.,!?()]+$", RegexOptions.Compiled);
        private static readonly Regex NodeIdPattern = new(@"^[a-zA-Z0-9\-_.]+$", RegexOptions.Compiled);
        private static readonly Regex SqlInjectionPattern = new(@"(?i)(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b.*\b(TABLE|DATABASE|INDEX|VIEW|PROCEDURE|FUNCTION|TRIGGER|SCHEMA)\b)|(\b(OR|AND)\b.*(=|>|<|\bLIKE\b))|(\b(SCRIPT|JAVASCRIPT|VBSCRIPT|ONLOAD|ONERROR)\b)|(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b.*\b(FROM|INTO|WHERE|SET|VALUES)\b)|(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b.*\b(.*\b(OR|AND)\b.*=.*\b(OR|AND)\b.*=.*))", RegexOptions.Compiled);
        private static readonly Regex XssPattern = new(@"(?i)(<script|</script|javascript:|vbscript:|onload=|onerror=|onclick=|onmouseover=)", RegexOptions.Compiled);

        public InputValidator(ICodexLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regexCache = new Dictionary<string, Regex>();
        }

        /// <summary>
        /// Validates a string input for basic security and format requirements
        /// </summary>
        public InputValidationResult ValidateString(string input, string fieldName, int maxLength = 1000, bool allowEmpty = false)
        {
            if (string.IsNullOrEmpty(input))
            {
                if (allowEmpty)
                    return InputValidationResult.Success;
                
                return new InputValidationResult($"{fieldName} cannot be null or empty");
            }

            if (input.Length > maxLength)
            {
                return new InputValidationResult($"{fieldName} exceeds maximum length of {maxLength} characters");
            }

            // Check for SQL injection patterns (but skip for legitimate API endpoints)
            if (fieldName == "PathSegment")
            {
                _logger.Info($"[DEBUG] Validating PathSegment: '{input}'");
                if (IsLegitimateApiEndpoint(input))
                {
                    _logger.Info($"[DEBUG] PathSegment '{input}' is legitimate, skipping SQL injection check");
                    return InputValidationResult.Success;
                }
                else
                {
                    _logger.Info($"[DEBUG] PathSegment '{input}' is not legitimate, checking for SQL injection");
                }
            }
            
            if (SqlInjectionPattern.IsMatch(input))
            {
                _logger.Warn($"Potential SQL injection detected in {fieldName}: {input.Substring(0, Math.Min(50, input.Length))}...");
                return new InputValidationResult($"{fieldName} contains potentially malicious content");
            }

            // Check for XSS patterns
            if (XssPattern.IsMatch(input))
            {
                _logger.Warn($"Potential XSS detected in {fieldName}: {input.Substring(0, Math.Min(50, input.Length))}...");
                return new InputValidationResult($"{fieldName} contains potentially malicious content");
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Checks if a path segment is a legitimate API endpoint that should be excluded from SQL injection checks
        /// </summary>
        private bool IsLegitimateApiEndpoint(string pathSegment)
        {
            var legitimateEndpoints = new[]
            {
                "create", "update", "delete", "get", "list", "search", "find", "add", "remove", "edit", "modify",
                "concept", "concepts", "energy", "identity", "user", "users", "node", "nodes", "edge", "edges",
                "auth", "login", "register", "logout", "profile", "settings", "config", "health", "status",
                "api", "swagger", "openapi", "docs", "metrics", "performance", "cache", "storage"
            };

            var isLegitimate = legitimateEndpoints.Contains(pathSegment.ToLowerInvariant());
            _logger.Debug($"IsLegitimateApiEndpoint check: pathSegment='{pathSegment}' (lowered='{pathSegment.ToLowerInvariant()}'), result={isLegitimate}");
            return isLegitimate;
        }

        /// <summary>
        /// Validates an email address
        /// </summary>
        public InputValidationResult ValidateEmail(string email, string fieldName = "Email")
        {
            if (string.IsNullOrEmpty(email))
            {
                return new InputValidationResult($"{fieldName} cannot be null or empty");
            }

            if (email.Length > 254) // RFC 5321 limit
            {
                return new InputValidationResult($"{fieldName} exceeds maximum length of 254 characters");
            }

            if (!EmailPattern.IsMatch(email))
            {
                return new InputValidationResult($"{fieldName} is not a valid email address");
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Validates a node ID
        /// </summary>
        public InputValidationResult ValidateNodeId(string nodeId, string fieldName = "NodeId")
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return new InputValidationResult($"{fieldName} cannot be null or empty");
            }

            if (nodeId.Length > 100)
            {
                return new InputValidationResult($"{fieldName} exceeds maximum length of 100 characters");
            }

            if (!NodeIdPattern.IsMatch(nodeId))
            {
                return new InputValidationResult($"{fieldName} contains invalid characters. Only alphanumeric, hyphens, underscores, and dots are allowed");
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Validates a numeric input
        /// </summary>
        public InputValidationResult ValidateNumber<T>(T value, string fieldName, T? minValue = null, T? maxValue = null) where T : struct, IComparable<T>
        {
            if (minValue.HasValue && value.CompareTo(minValue.Value) < 0)
            {
                return new InputValidationResult($"{fieldName} must be greater than or equal to {minValue.Value}");
            }

            if (maxValue.HasValue && value.CompareTo(maxValue.Value) > 0)
            {
                return new InputValidationResult($"{fieldName} must be less than or equal to {maxValue.Value}");
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Validates a collection input
        /// </summary>
        public InputValidationResult ValidateCollection<T>(IEnumerable<T> collection, string fieldName, int maxCount = 1000, bool allowEmpty = false)
        {
            if (collection == null)
            {
                return new InputValidationResult($"{fieldName} cannot be null");
            }

            var items = collection.ToList();
            
            if (items.Count == 0 && !allowEmpty)
            {
                return new InputValidationResult($"{fieldName} cannot be empty");
            }

            if (items.Count > maxCount)
            {
                return new InputValidationResult($"{fieldName} exceeds maximum count of {maxCount} items");
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Validates a JSON string
        /// </summary>
        public InputValidationResult ValidateJson(string json, string fieldName = "Json")
        {
            if (string.IsNullOrEmpty(json))
            {
                return new InputValidationResult($"{fieldName} cannot be null or empty");
            }

            try
            {
                System.Text.Json.JsonDocument.Parse(json);
                return InputValidationResult.Success;
            }
            catch (System.Text.Json.JsonException ex)
            {
                return new InputValidationResult($"{fieldName} is not valid JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates a URL
        /// </summary>
        public InputValidationResult ValidateUrl(string url, string fieldName = "Url")
        {
            if (string.IsNullOrEmpty(url))
            {
                return new InputValidationResult($"{fieldName} cannot be null or empty");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return new InputValidationResult($"{fieldName} is not a valid URL");
            }

            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return new InputValidationResult($"{fieldName} must use HTTP or HTTPS protocol");
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Sanitizes a string by removing potentially dangerous characters
        /// </summary>
        public string SanitizeString(string input, bool preserveSpaces = true)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove or replace potentially dangerous characters
            var sanitized = input;
            
            // Remove script tags and their content
            sanitized = Regex.Replace(sanitized, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
            
            // Remove other potentially dangerous HTML tags
            sanitized = Regex.Replace(sanitized, @"<[^>]*>", "");
            
            // Remove control characters except newlines and tabs
            sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // Optionally preserve spaces
            if (!preserveSpaces)
            {
                sanitized = sanitized.Trim();
            }

            return sanitized;
        }

        /// <summary>
        /// Validates multiple fields at once
        /// </summary>
        public InputValidationResult ValidateMultiple(params InputValidationResult[] results)
        {
            var errors = results.Where(r => !r.IsValid).ToList();
            
            if (errors.Any())
            {
                var errorMessages = string.Join("; ", errors.Select(e => e.ErrorMessage));
                return new InputValidationResult(errorMessages);
            }

            return InputValidationResult.Success;
        }

        /// <summary>
        /// Validates request rate limiting
        /// </summary>
        public InputValidationResult ValidateRateLimit(string clientId, int maxRequestsPerMinute = 60)
        {
            // This is a simplified implementation
            // In production, you'd use a proper rate limiting service like Redis
            // For now, we'll just return success
            return InputValidationResult.Success;
        }

        /// <summary>
        /// Validates file upload
        /// </summary>
        public InputValidationResult ValidateFileUpload(string fileName, long fileSize, string[] allowedExtensions = null, long maxSizeBytes = 10 * 1024 * 1024) // 10MB default
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return new InputValidationResult("File name cannot be null or empty");
            }

            if (fileSize > maxSizeBytes)
            {
                return new InputValidationResult($"File size exceeds maximum allowed size of {maxSizeBytes / (1024 * 1024)}MB");
            }

            if (allowedExtensions != null && allowedExtensions.Length > 0)
            {
                var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return new InputValidationResult($"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                }
            }

            // Check for potentially dangerous file names
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return new InputValidationResult("File name contains potentially dangerous characters");
            }

            return InputValidationResult.Success;
        }
    }

    /// <summary>
    /// Interface for input validation
    /// </summary>
    public interface IInputValidator
    {
        InputValidationResult ValidateString(string input, string fieldName, int maxLength = 1000, bool allowEmpty = false);
        InputValidationResult ValidateEmail(string email, string fieldName = "Email");
        InputValidationResult ValidateNodeId(string nodeId, string fieldName = "NodeId");
        InputValidationResult ValidateNumber<T>(T value, string fieldName, T? minValue = null, T? maxValue = null) where T : struct, IComparable<T>;
        InputValidationResult ValidateCollection<T>(IEnumerable<T> collection, string fieldName, int maxCount = 1000, bool allowEmpty = false);
        InputValidationResult ValidateJson(string json, string fieldName = "Json");
        InputValidationResult ValidateUrl(string url, string fieldName = "Url");
        string SanitizeString(string input, bool preserveSpaces = true);
        InputValidationResult ValidateMultiple(params InputValidationResult[] results);
        InputValidationResult ValidateRateLimit(string clientId, int maxRequestsPerMinute = 60);
        InputValidationResult ValidateFileUpload(string fileName, long fileSize, string[] allowedExtensions = null, long maxSizeBytes = 10 * 1024 * 1024);
    }

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class InputValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        private InputValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public InputValidationResult(string errorMessage)
        {
            IsValid = false;
            ErrorMessage = errorMessage;
        }

        public static InputValidationResult Success => new(true);
        public static InputValidationResult Error(string message) => new(false, message);

        public static implicit operator bool(InputValidationResult result) => result.IsValid;
    }
}
