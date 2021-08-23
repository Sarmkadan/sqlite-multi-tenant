#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace SqliteMultiTenant.Formatters;

/// <summary>
/// Interface for different output formatters (JSON, CSV, XML).
/// Enables pluggable serialization for API responses and file exports.
/// </summary>
public interface IOutputFormatter
{
    string Format<T>(T data);
    string ContentType { get; }
}

/// <summary>
/// JSON formatter for API responses and data exports.
/// Implements camelCase property naming and indentation for readability.
/// </summary>
public sealed class JsonFormatter : IOutputFormatter {
    public string ContentType => "application/json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serializes object to JSON string with consistent formatting.
    /// Handles null gracefully without throwing.
    /// </summary>
    public string Format<T>(T data)
    {
        try
        {
            return JsonSerializer.Serialize(data, Options);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, Options);
        }
    }
}

/// <summary>
/// CSV formatter for exporting tabular data (backups, migrations, tenants).
/// Handles escaping of special characters and quote wrapping.
/// </summary>
public sealed class CsvFormatter : IOutputFormatter {
    public string ContentType => "text/csv";

    /// <summary>
    /// Formats collection as CSV with headers.
    /// Supports any IEnumerable<T> where T has public properties.
    /// Escapes commas, quotes, and newlines in values.
    /// </summary>
    public string Format<T>(T data)
    {
        try
        {
            if (data is IEnumerable<object> collection)
                return FormatCollection(collection);

            if (data is object obj)
                return FormatSingleObject(obj);

            return string.Empty;
        }
        catch (Exception ex)
        {
            return $"Error formatting CSV: {ex.Message}";
        }
    }

    /// <summary>
    /// Formats collection of objects as CSV with headers.
    /// First row contains property names, subsequent rows contain values.
    /// </summary>
    private string FormatCollection(IEnumerable<object> collection)
    {
        var items = collection.ToList();
        if (items.Count == 0)
            return string.Empty;

        var firstItem = items.First();
        var properties = firstItem.GetType().GetProperties();

        var header = string.Join(",", properties.Select(p => EscapeCsvField(p.Name)));
        var rows = new List<string> { header };

        foreach (var item in items)
        {
            var values = properties.Select(p => EscapeCsvField(p.GetValue(item)?.ToString() ?? string.Empty));
            rows.Add(string.Join(",", values));
        }

        return string.Join(Environment.NewLine, rows);
    }

    private string FormatSingleObject(object obj)
    {
        var properties = obj.GetType().GetProperties();
        var lines = new List<string> { "Property,Value" };

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj)?.ToString() ?? string.Empty;
            lines.Add($"{EscapeCsvField(prop.Name)},{EscapeCsvField(value)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Escapes CSV field values according to RFC 4180.
    /// Wraps fields containing commas, quotes, or newlines.
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (!field.Contains(",") && !field.Contains("\"") && !field.Contains("\n"))
            return field;

        // Escape quotes by doubling them and wrap in quotes
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}

/// <summary>
/// XML formatter for structured data exports and integration scenarios.
/// Generates well-formed XML with root element wrapper.
/// </summary>
public sealed class XmlFormatter : IOutputFormatter {
    public string ContentType => "application/xml";

    /// <summary>
    /// Formats object as XML string.
    /// Single objects wrapped in object-specific element, collections in Items wrapper.
    /// </summary>
    public string Format<T>(T data)
    {
        try
        {
            XElement root = data switch
            {
                IEnumerable<object> collection => FormatCollection(collection),
                _ => FormatSingleObject(data)
            };

            return root.ToString();
        }
        catch (Exception ex)
        {
            return $"<Error>{System.Net.WebUtility.HtmlEncode(ex.Message)}</Error>";
        }
    }

    /// <summary>
    /// Converts collection to XML with Items root element.
    /// Each item becomes Item element with properties as child elements.
    /// </summary>
    private XElement FormatCollection(IEnumerable<object> collection)
    {
        var root = new XElement("Items");

        foreach (var item in collection)
        {
            var itemElement = new XElement(item.GetType().Name);
            var properties = item.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(item);
                itemElement.Add(new XElement(prop.Name, value ?? string.Empty));
            }

            root.Add(itemElement);
        }

        return root;
    }

    /// <summary>
    /// Converts single object to XML element.
    /// </summary>
    private XElement FormatSingleObject<T>(T obj)
    {
        var element = new XElement(obj.GetType().Name);
        var properties = obj.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            element.Add(new XElement(prop.Name, value ?? string.Empty));
        }

        return element;
    }
}

/// <summary>
/// Factory for selecting formatter based on content type or file extension.
/// </summary>
public sealed class FormatterFactory {
    private readonly Dictionary<string, IOutputFormatter> _formatters;

    public FormatterFactory()
    {
        _formatters = new Dictionary<string, IOutputFormatter>(StringComparer.OrdinalIgnoreCase)
        {
            { "json", new JsonFormatter() },
            { "csv", new CsvFormatter() },
            { "xml", new XmlFormatter() }
        };
    }

    /// <summary>
    /// Gets formatter by type name (json, csv, xml).
    /// Defaults to JSON if type not found.
    /// </summary>
    public IOutputFormatter GetFormatter(string type)
    {
        return _formatters.TryGetValue(type, out var formatter)
            ? formatter
            : new JsonFormatter();
    }

    /// <summary>
    /// Gets formatter by content type (application/json, text/csv, etc).
    /// </summary>
    public IOutputFormatter GetFormatterByContentType(string contentType)
    {
        return contentType switch
        {
            "text/csv" => new CsvFormatter(),
            "application/xml" => new XmlFormatter(),
            _ => new JsonFormatter()
        };
    }
}

/// <summary>
/// General-purpose formatter used by CLI output paths.
/// Renders objects as plain text or delegates to <see cref="FormatterFactory"/> for
/// json/csv/xml output.
/// </summary>
public sealed class OutputFormatter {
    private readonly FormatterFactory _formatterFactory;

    public OutputFormatter()
        : this(new FormatterFactory())
    {
    }

    public OutputFormatter(FormatterFactory formatterFactory)
    {
        _formatterFactory = formatterFactory;
    }

    /// <summary>
    /// Formats a single object according to the requested format ("text", "json", "csv", "xml").
    /// "text" renders a simple Property: Value listing; other formats delegate to FormatterFactory.
    /// </summary>
    public string FormatObject(object data, string format)
    {
        if (data is null)
            return string.Empty;

        if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase))
        {
            var properties = data.GetType().GetProperties();
            var lines = properties.Select(p => $"{p.Name}: {p.GetValue(data) ?? string.Empty}");
            return string.Join(Environment.NewLine, lines);
        }

        return _formatterFactory.GetFormatter(format).Format(data);
    }
}
