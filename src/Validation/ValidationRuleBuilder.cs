#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Validation
{
    // Fluent API for building complex validation rules with composable conditions
    // Enables reusable validation logic across the application
    /// <summary>
    /// Provides a fluent API for building complex validation rules with composable conditions.
    /// </summary>
    /// <typeparam name="T">The type of object being validated.</typeparam>
    public sealed class ValidationRuleBuilder<T> : IEquatable<ValidationRuleBuilder<T>>
    {
        private readonly List<ValidationRule> _rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationRuleBuilder{T}"/> class.
        /// </summary>
        public ValidationRuleBuilder()
        {
            _rules = new List<ValidationRule>();
        }

        /// <summary>
        /// Adds a required field validation rule.
        /// </summary>
        /// <param name="fieldName">The name of the field to validate.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be used.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> Required(string fieldName, string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = obj => HasValue(obj, fieldName),
                ErrorMessage = message ?? $"{fieldName} is required"
            });

            return this;
        }

        /// <summary>
        /// Adds a string length validation rule with optional minimum and maximum length constraints.
        /// </summary>
        /// <param name="fieldName">The name of the field to validate.</param>
        /// <param name="minLength">Optional minimum length requirement. If null, no minimum length is enforced.</param>
        /// <param name="maxLength">Optional maximum length requirement. If null, no maximum length is enforced.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be generated based on the constraints.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> StringLength(string fieldName, int? minLength = null,
            int? maxLength = null, string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = obj =>
                {
                    var value = GetPropertyValue(obj, fieldName) as string;
                    if (string.IsNullOrEmpty(value)) return true;

                    if (minLength.HasValue && value.Length < minLength.Value) return false;
                    if (maxLength.HasValue && value.Length > maxLength.Value) return false;

                    return true;
                },
                ErrorMessage = message ?? BuildLengthMessage(fieldName, minLength, maxLength)
            });

            return this;
        }

        /// <summary>
        /// Adds an email format validation rule.
        /// </summary>
        /// <param name="fieldName">The name of the field containing the email address to validate.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be used.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> Email(string fieldName, string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = obj =>
                {
                    var value = GetPropertyValue(obj, fieldName) as string;
                    if (string.IsNullOrEmpty(value)) return true;

                    var pattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
                    return Regex.IsMatch(value, pattern);
                },
                ErrorMessage = message ?? $"{fieldName} must be a valid email address"
            });

            return this;
        }

        /// <summary>
        /// Adds a numeric range validation rule with optional minimum and maximum value constraints.
        /// </summary>
        /// <param name="fieldName">The name of the field containing the numeric value to validate.</param>
        /// <param name="minValue">Optional minimum value requirement. If null, no minimum value is enforced.</param>
        /// <param name="maxValue">Optional maximum value requirement. If null, no maximum value is enforced.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be generated based on the constraints.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> Range(string fieldName, object? minValue = null,
            object? maxValue = null, string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = obj =>
                {
                    var value = GetPropertyValue(obj, fieldName);
                    if (value is null) return true;

                    try
                    {
                        var numValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                        if (minValue is not null && numValue < Convert.ToDecimal(minValue, CultureInfo.InvariantCulture)) return false;
                        if (maxValue is not null && numValue > Convert.ToDecimal(maxValue, CultureInfo.InvariantCulture)) return false;
                        return true;
                    }
                    catch { return false; }
                },
                ErrorMessage = message ?? BuildRangeMessage(fieldName, minValue, maxValue)
            });

            return this;
        }

        /// <summary>
        /// Adds a regex pattern validation rule.
        /// </summary>
        /// <param name="fieldName">The name of the field to validate against the pattern.</param>
        /// <param name="pattern">The regular expression pattern to match against the field value.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be used.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> Pattern(string fieldName, string pattern, string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = obj =>
                {
                    var value = GetPropertyValue(obj, fieldName) as string;
                    if (string.IsNullOrEmpty(value)) return true;

                    return Regex.IsMatch(value, pattern);
                },
                ErrorMessage = message ?? $"{fieldName} does not match the required pattern"
            });

            return this;
        }

        /// <summary>
        /// Adds a custom validation predicate.
        /// </summary>
        /// <param name="fieldName">The name of the field to validate.</param>
        /// <param name="predicate">A custom predicate function that returns true if validation passes, false otherwise.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be used.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> Custom(string fieldName, Func<object, bool> predicate,
            string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = predicate,
                ErrorMessage = message ?? $"{fieldName} validation failed"
            });

            return this;
        }

        /// <summary>
        /// Adds a cross-field validation rule that ensures two fields have matching values.
        /// </summary>
        /// <param name="field1">The name of the first field to compare.</param>
        /// <param name="field2">The name of the second field to compare.</param>
        /// <param name="message">Optional custom error message. If null, a default message will be used.</param>
        /// <returns>The current <see cref="ValidationRuleBuilder{T}"/> instance for method chaining.</returns>
        public ValidationRuleBuilder<T> MustMatch(string field1, string field2, string? message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = $"{field1},{field2}",
                Predicate = obj =>
                {
                    var val1 = GetPropertyValue(obj, field1);
                    var val2 = GetPropertyValue(obj, field2);
                    return Equals(val1, val2);
                },
                ErrorMessage = message ?? $"{field1} and {field2} must match"
            });

            return this;
        }

        /// <summary>
        /// Validates the specified object against all registered validation rules.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        /// <returns>A <see cref="RuleValidationResult"/> indicating whether validation succeeded and containing any errors.</returns>
        public RuleValidationResult Validate(T obj)
        {
            var errors = new List<RuleValidationError>();

            foreach (var rule in _rules)
            {
                try
                {
                    if (!rule.Predicate(obj))
                    {
                        errors.Add(new RuleValidationError
                        {
                            FieldName = rule.FieldName,
                            Message = rule.ErrorMessage
                        });
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new RuleValidationError
                    {
                        FieldName = rule.FieldName,
                        Message = $"Validation error: {ex.Message}"
                    });
                }
            }

            return new RuleValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        #region Equality members

        public bool Equals(ValidationRuleBuilder<T>? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (_rules.Count != other._rules.Count) return false;

            for (int i = 0; i < _rules.Count; i++)
            {
                var a = _rules[i];
                var b = other._rules[i];

                if (a.FieldName != b.FieldName) return false;
                if (!Equals(a.Predicate, b.Predicate)) return false;
                if (a.ErrorMessage != b.ErrorMessage) return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as ValidationRuleBuilder<T>);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(_rules.Count);
            foreach (var r in _rules)
            {
                hash.Add(r.FieldName);
                hash.Add(r.Predicate);
                hash.Add(r.ErrorMessage);
            }
            return hash.ToHashCode();
        }

        public static bool operator ==(ValidationRuleBuilder<T>? left, ValidationRuleBuilder<T>? right) => Equals(left, right);

        public static bool operator !=(ValidationRuleBuilder<T>? left, ValidationRuleBuilder<T>? right) => !Equals(left, right);

        #endregion

        private bool HasValue(object obj, string fieldName)
        {
            var value = GetPropertyValue(obj, fieldName);

            if (value is null) return false;
            if (value is string str) return !string.IsNullOrWhiteSpace(str);

            return true;
        }

        private object? GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj?.GetType().GetProperty(propertyName);
                return property?.GetValue(obj);
            }
            catch { return null; }
        }

        private string BuildLengthMessage(string fieldName, int? minLength, int? maxLength)
        {
            if (minLength.HasValue && maxLength.HasValue)
                return $"{fieldName} must be between {minLength} and {maxLength} characters";

            if (minLength.HasValue)
                return $"{fieldName} must be at least {minLength} characters";

            if (maxLength.HasValue)
                return $"{fieldName} must not exceed {maxLength} characters";

            return $"{fieldName} length is invalid";
        }

        private string BuildRangeMessage(string fieldName, object? minValue, object? maxValue)
        {
            if (minValue is not null && maxValue is not null)
                return $"{fieldName} must be between {minValue} and {maxValue}";

            if (minValue is not null)
                return $"{fieldName} must be at least {minValue}";

            if (maxValue is not null)
                return $"{fieldName} must not exceed {maxValue}";

            return $"{fieldName} is out of range";
        }

        private class ValidationRule
        {
            public string? FieldName { get; set; }
            public Func<object, bool>? Predicate { get; set; }
            public string? ErrorMessage { get; set; }
        }
    }

    /// <summary>
    /// Represents the result of a validation operation, including success flag and any errors.
    /// </summary>
    public sealed class RuleValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation succeeded.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public List<RuleValidationError> Errors { get; set; } = new List<RuleValidationError>();
    }

    /// <summary>
    /// Represents a single validation error for a specific field.
    /// </summary>
    public sealed class RuleValidationError
    {
        /// <summary>
        /// Gets or sets the name of the field that failed validation.
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// Gets or sets the error message describing the validation failure.
        /// </summary>
        public string? Message { get; set; }
    }
}
