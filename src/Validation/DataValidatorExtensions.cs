#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Validation;

/// <summary>
/// Extension methods for <see cref="DataValidator"/> providing additional validation capabilities.
/// </summary>
public static class DataValidatorExtensions
{
    /// <summary>
    /// Validates that a string is not null or empty with a custom error message.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireString(
        this DataValidator validator,
        string? value,
        string fieldName,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(errorMessage);

        if (string.IsNullOrWhiteSpace(value))
            validator.GetErrors().Add(new ValidationError(fieldName, errorMessage));

        return validator;
    }

    /// <summary>
    /// Validates that a string has a minimum length.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minLength is negative.</exception>
    public static DataValidator RequireMinLength(
        this DataValidator validator,
        string? value,
        int minLength,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);

        if (!string.IsNullOrWhiteSpace(value) && value.Length < minLength)
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be at least {minLength} characters long"));

        return validator;
    }

    /// <summary>
    /// Validates that a string matches a minimum and maximum length.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minLength is negative or maxLength is less than minLength.</exception>
    public static DataValidator RequireLengthBetween(
        this DataValidator validator,
        string? value,
        int minLength,
        int maxLength,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);

        if (!string.IsNullOrWhiteSpace(value))
        {
            var length = value.Length;
            if (length < minLength || length > maxLength)
                validator.GetErrors().Add(new ValidationError(
                    fieldName,
                    $"{fieldName} must be between {minLength} and {maxLength} characters long"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a string is a valid phone number in international format.
    /// Supports formats like +1234567890, +1 (234) 567-8901, etc.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireValidPhoneNumber(
        this DataValidator validator,
        string? phoneNumber,
        string fieldName = "PhoneNumber")
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            validator.GetErrors().Add(new ValidationError(fieldName, $"{fieldName} is required"));
            return validator;
        }

        // Basic international phone number validation (E.164 format)
        // Pattern: + followed by 8-15 digits, optional spaces, dashes, or parentheses
        var phonePattern = @"^\+[0-9]{8,15}([\s\-\(\)]?[0-9]{1,4})*$";
        if (!Regex.IsMatch(phoneNumber, phonePattern))
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be a valid international phone number (e.g., +1234567890)"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a string is a valid date in ISO 8601 format (YYYY-MM-DD).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireValidDate(
        this DataValidator validator,
        string? dateString,
        string fieldName = "Date")
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (string.IsNullOrWhiteSpace(dateString))
        {
            validator.GetErrors().Add(new ValidationError(fieldName, $"{fieldName} is required"));
            return validator;
        }

        if (!DateOnly.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be a valid date in YYYY-MM-DD format"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a string is a valid date/time in ISO 8601 format (YYYY-MM-DDTHH:MM:SS).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireValidDateTime(
        this DataValidator validator,
        string? dateTimeString,
        string fieldName = "DateTime")
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (string.IsNullOrWhiteSpace(dateTimeString))
        {
            validator.GetErrors().Add(new ValidationError(fieldName, $"{fieldName} is required"));
            return validator;
        }

        if (!DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be a valid date/time in YYYY-MM-DDTHH:MM:SS format"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a string is a valid IPv4 address.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireValidIPv4(
        this DataValidator validator,
        string? ipAddress,
        string fieldName = "IPAddress")
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            validator.GetErrors().Add(new ValidationError(fieldName, $"{fieldName} is required"));
            return validator;
        }

        var parts = ipAddress.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || parts.Any(p => !int.TryParse(p, out var num) || num < 0 || num > 255))
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be a valid IPv4 address (e.g., 192.168.1.1)"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a collection has exactly the specified number of items.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when expectedCount is negative.</exception>
    public static DataValidator RequireCollectionCount<T>(
        this DataValidator validator,
        IEnumerable<T>? collection,
        int expectedCount,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(expectedCount);

        if (collection is null || collection.Count() != expectedCount)
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must contain exactly {expectedCount} items"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a collection has at most the specified number of items.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxCount is negative.</exception>
    public static DataValidator RequireMaxItems<T>(
        this DataValidator validator,
        IEnumerable<T>? collection,
        int maxCount,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCount);

        if (collection is not null && collection.Count() > maxCount)
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must contain at most {maxCount} items"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a value is greater than another value.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireGreaterThan<T>(
        this DataValidator validator,
        T? value,
        T? minimumValue,
        string fieldName) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (value is not null && minimumValue is not null && value.CompareTo(minimumValue) <= 0)
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be greater than {minimumValue}"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a value is less than another value.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireLessThan<T>(
        this DataValidator validator,
        T? value,
        T? maximumValue,
        string fieldName) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (value is not null && maximumValue is not null && value.CompareTo(maximumValue) >= 0)
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be less than {maximumValue}"));
        }

        return validator;
    }

    /// <summary>
    /// Validates that a string is a valid credit card number (basic Luhn check).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when fieldName is null.</exception>
    public static DataValidator RequireValidCreditCard(
        this DataValidator validator,
        string? cardNumber,
        string fieldName = "CreditCardNumber")
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fieldName);

        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            validator.GetErrors().Add(new ValidationError(fieldName, $"{fieldName} is required"));
            return validator;
        }

        // Remove all non-digit characters
        var digitsOnly = new string(cardNumber.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be between 13 and 19 digits long"));
            return validator;
        }

        // Basic Luhn algorithm check
        if (!IsValidLuhn(digitsOnly))
        {
            validator.GetErrors().Add(new ValidationError(
                fieldName,
                $"{fieldName} must be a valid credit card number"));
        }

        return validator;
    }

    private static bool IsValidLuhn(string digits)
    {
        var sum = 0;
        var alternate = false;

        // Working from rightmost digit
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            if (!int.TryParse(digits[i].ToString(), out var digit))
                return false;

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit = (digit % 10) + 1;
            }

            sum += digit;
            alternate = !alternate;
        }

        return (sum % 10 == 0);
    }

    /// <summary>
    /// Gets the list of validation errors from the validator.
    /// </summary>
    private static List<ValidationError> GetErrors(this DataValidator validator)
    {
        // Use reflection to access the private _errors field
        var field = typeof(DataValidator).GetField(
            "_errors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field?.GetValue(validator) is List<ValidationError> errors)
        {
            return errors;
        }

        throw new InvalidOperationException("Could not access validation errors collection");
    }
}