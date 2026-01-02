// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Validation;

/// <summary>
/// Comprehensive data validation service for input validation and sanitization.
/// Provides fluent validation API with custom validators and error messages.
/// </summary>
public class DataValidator
{
    private readonly List<ValidationError> _errors;
    private readonly ILogger<DataValidator> _logger;

    public DataValidator(ILogger<DataValidator> logger)
    {
        _logger = logger;
        _errors = new List<ValidationError>();
    }

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    public DataValidator RequireString(string? value, string fieldName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} is required"));
        else if (maxLength.HasValue && value.Length > maxLength)
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must not exceed {maxLength} characters"));

        return this;
    }

    /// <summary>
    /// Validates that an integer is within a specified range.
    /// </summary>
    public DataValidator RequireRange(int? value, int minValue, int maxValue, string fieldName)
    {
        if (!value.HasValue)
            _errors.Add(new ValidationError(fieldName, $"{fieldName} is required"));
        else if (value < minValue || value > maxValue)
            _errors.Add(new ValidationError(fieldName,
                $"{fieldName} must be between {minValue} and {maxValue}"));

        return this;
    }

    /// <summary>
    /// Validates email address format.
    /// </summary>
    public DataValidator RequireValidEmail(string? email, string fieldName = "Email")
    {
        if (string.IsNullOrWhiteSpace(email))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} is required"));
        else if (!IsValidEmail(email))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must be a valid email address"));

        return this;
    }

    /// <summary>
    /// Validates URL format.
    /// </summary>
    public DataValidator RequireValidUrl(string? url, string fieldName = "URL")
    {
        if (string.IsNullOrWhiteSpace(url))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} is required"));
        else if (!Uri.TryCreate(url, UriKind.Absolute, out var result) ||
                 (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must be a valid HTTP(S) URL"));

        return this;
    }

    /// <summary>
    /// Validates that a GUID is not empty.
    /// </summary>
    public DataValidator RequireValidGuid(string? guid, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(guid) || !Guid.TryParse(guid, out _))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must be a valid GUID"));

        return this;
    }

    /// <summary>
    /// Validates against a custom regex pattern.
    /// </summary>
    public DataValidator RequirePattern(string? value, string pattern, string fieldName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (!Regex.IsMatch(value, pattern))
                _errors.Add(new ValidationError(fieldName, message));
        }

        return this;
    }

    /// <summary>
    /// Validates using a custom validation function.
    /// </summary>
    public DataValidator Require<T>(T? value, Func<T?, bool> predicate, string fieldName, string message)
    {
        if (!predicate(value))
            _errors.Add(new ValidationError(fieldName, message));

        return this;
    }

    /// <summary>
    /// Validates that a collection has at least one item.
    /// </summary>
    public DataValidator RequireNotEmpty<T>(IEnumerable<T>? collection, string fieldName)
    {
        if (collection == null || !collection.Any())
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must contain at least one item"));

        return this;
    }

    /// <summary>
    /// Validates that the value equals another value.
    /// </summary>
    public DataValidator RequireEqual<T>(T? value, T? expectedValue, string fieldName) where T : class
    {
        if (value != expectedValue)
            _errors.Add(new ValidationError(fieldName, $"{fieldName} does not match expected value"));

        return this;
    }

    /// <summary>
    /// Validates that the value does not equal another value.
    /// </summary>
    public DataValidator RequireNotEqual<T>(T? value, T? unexpectedValue, string fieldName) where T : class
    {
        if (value == unexpectedValue)
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must not equal the given value"));

        return this;
    }

    /// <summary>
    /// Gets validation result.
    /// </summary>
    public ValidationResult GetResult()
    {
        return new ValidationResult
        {
            IsValid = !_errors.Any(),
            Errors = new List<ValidationError>(_errors)
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class ValidationError
{
    public string FieldName { get; set; }
    public string Message { get; set; }

    public ValidationError(string fieldName, string message)
    {
        FieldName = fieldName;
        Message = message;
    }

    public override string ToString() => $"{FieldName}: {Message}";
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();

    public string GetErrorMessage()
    {
        return string.Join("; ", Errors.Select(e => e.ToString()));
    }
}
