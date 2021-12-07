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
    public sealed class ValidationRuleBuilder<T> {
        private readonly List<ValidationRule> _rules;

        public ValidationRuleBuilder()
        {
            _rules = new List<ValidationRule>();
        }

        // Adds required field validation
        public ValidationRuleBuilder<T> Required(string fieldName, string message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = obj => HasValue(obj, fieldName),
                ErrorMessage = message ?? $"{fieldName} is required"
            });

            return this;
        }

        // Validates string length constraints
        public ValidationRuleBuilder<T> StringLength(string fieldName, int? minLength = null,
            int? maxLength = null, string message = null)
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

        // Validates email format
        public ValidationRuleBuilder<T> Email(string fieldName, string message = null)
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

        // Validates numeric range
        public ValidationRuleBuilder<T> Range(string fieldName, object minValue = null,
            object maxValue = null, string message = null)
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

        // Validates regex pattern match
        public ValidationRuleBuilder<T> Pattern(string fieldName, string pattern, string message = null)
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

        // Custom validation predicate
        public ValidationRuleBuilder<T> Custom(string fieldName, Func<object, bool> predicate,
            string message = null)
        {
            _rules.Add(new ValidationRule
            {
                FieldName = fieldName,
                Predicate = predicate,
                ErrorMessage = message ?? $"{fieldName} validation failed"
            });

            return this;
        }

        // Cross-field validation
        public ValidationRuleBuilder<T> MustMatch(string field1, string field2, string message = null)
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

        // Builds and returns the validation result
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

        private bool HasValue(object obj, string fieldName)
        {
            var value = GetPropertyValue(obj, fieldName);

            if (value is null) return false;
            if (value is string str) return !string.IsNullOrWhiteSpace(str);

            return true;
        }

        private object GetPropertyValue(object obj, string propertyName)
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

        private string BuildRangeMessage(string fieldName, object minValue, object maxValue)
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
            public string FieldName { get; set; }
            public Func<object, bool> Predicate { get; set; }
            public string ErrorMessage { get; set; }
        }
    }

    public sealed class RuleValidationResult {
        public bool IsValid { get; set; }
        public List<RuleValidationError> Errors { get; set; } = new List<RuleValidationError>();
    }

    public sealed class RuleValidationError {
        public string FieldName { get; set; }
        public string Message { get; set; }
    }
}
