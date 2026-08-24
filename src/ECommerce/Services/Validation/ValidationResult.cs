using ECommerce.Common;

namespace ECommerce.Services.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }
        public ServiceErrorType ErrorType { get; init; }

        public static ValidationResult Valid() => new() { IsValid = true };

        public static ValidationResult Invalid(string message, ServiceErrorType type) =>
            new() { IsValid = false, ErrorMessage = message, ErrorType = type };
    }
}
