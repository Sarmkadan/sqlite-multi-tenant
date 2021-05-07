#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections;
using System.Reflection;

namespace SqliteMultiTenant.Formatters;

/// <summary>
/// Formats objects and collections as CSV (Comma-Separated Values).
/// Handles nested objects, arrays, and special character escaping.
/// Useful for exporting data to spreadsheet applications.
/// </summary>
public sealed class CsvFormatter {
    private readonly string _delimiter;
    private readonly bool _includeHeader;
    private readonly ILogger<CsvFormatter> _logger;

    public CsvFormatter(ILogger<CsvFormatter> logger, string delimiter = ",", bool includeHeader = true)
    {
        _delimiter = delimiter;
        _includeHeader = includeHeader;
        _logger = logger;
    }

    /// <summary>
    /// Formats an object as CSV.
    /// For single objects, returns one row with properties as columns.
    /// For collections, returns multiple rows.
    /// </summary>
    public string Format<T>(T? data) where T : class
    {
        try
        {
            if (data is null)
                return string.Empty;

            if (data is IEnumerable enumerable && !(data is string))
                return FormatCollection(enumerable);

            return FormatObject(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"CSV formatting error: {ex.Message}");
            return string.Empty;
        }
    }

    private string FormatCollection(IEnumerable collection)
    {
        var rows = new List<string>();
        var properties = new PropertyInfo[] { };
        bool headerAdded = false;

        foreach (var item in collection)
        {
            if (item is null)
                continue;

            var itemType = item.GetType();

            // Get properties from first item
            if (!headerAdded)
            {
                properties = GetProperties(itemType);

                // Add header row
                if (_includeHeader)
                {
                    var headerValues = properties.Select(p => EscapeValue(p.Name));
                    rows.Add(string.Join(_delimiter, headerValues));
                }

                headerAdded = true;
            }

            // Add data row
            var rowValues = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeValue(value?.ToString() ?? string.Empty);
            });

            rows.Add(string.Join(_delimiter, rowValues));
        }

        return string.Join(Environment.NewLine, rows);
    }

    private string FormatObject<T>(T obj) where T : class
    {
        var rows = new List<string>();
        var properties = GetProperties(typeof(T));

        // Header
        if (_includeHeader)
        {
            var headerValues = properties.Select(p => EscapeValue(p.Name));
            rows.Add(string.Join(_delimiter, headerValues));
        }

        // Data row
        var dataValues = properties.Select(p =>
        {
            var value = p.GetValue(obj);
            return EscapeValue(value?.ToString() ?? string.Empty);
        });

        rows.Add(string.Join(_delimiter, dataValues));

        return string.Join(Environment.NewLine, rows);
    }

    private PropertyInfo[] GetProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
            .ToArray();
    }

    private string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape double quotes and wrap in quotes if contains special characters
        if (value.Contains(_delimiter) || value.Contains("\"") || value.Contains("\n"))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }
}
