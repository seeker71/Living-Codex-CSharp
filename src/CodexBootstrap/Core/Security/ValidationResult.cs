using System;

namespace CodexBootstrap.Core.Security
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public ValidationResult(string errorMessage)
        {
            IsValid = false;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success => new(true);
        public static ValidationResult Error(string message) => new(false, message);

        public static implicit operator bool(ValidationResult result) => result.IsValid;
    }
}
