#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections;
using System.Reflection;
using System.Xml.Linq;

namespace SqliteMultiTenant.Formatters;

/// <summary>
/// Formats objects and collections as XML.
/// Handles nested objects, arrays, and preserves type information.
/// Useful for system integration and data interchange.
/// </summary>
public sealed class XmlExportFormatter {
    private readonly bool _includeDeclaration;
    private readonly ILogger<XmlExportFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlExportFormatter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="includeDeclaration">Whether to include the XML declaration.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public XmlExportFormatter(ILogger<XmlExportFormatter> logger, bool includeDeclaration = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger.LogInformation("Initializing XmlExportFormatter with {IncludeDeclaration} and logger {Logger}", includeDeclaration, logger);
        _includeDeclaration = includeDeclaration;
        _logger = logger;
    }

    /// <summary>
    /// Formats an object or collection as XML.
    /// Creates root element and recursively builds nested XML structure.
    /// </summary>
    /// <param name="data">The data to format.</param>
    /// <param name="rootName">The name of the root element.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rootName"/> is null or whitespace.</exception>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <returns>The formatted XML string.</returns>
    public string Format<T>(T? data, string rootName = "root") where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootName);
        try
        {
            if (data is null)
                return BuildXmlString(new XElement(rootName));

            XElement root;

            if (data is IEnumerable enumerable && !(data is string))
                root = FormatCollection(enumerable, rootName);
            else
                root = FormatObject(data, rootName);

            return BuildXmlString(root);
        }
        catch (Exception ex)
        {
            _logger.LogError("XML formatting error: {Message}", ex.Message);
            return $"<error>{EscapeXml(ex.Message)}</error>";
        }
    }

    private XElement FormatCollection(IEnumerable collection, string rootName)
    {
        var root = new XElement(rootName);
        var itemName = "item";

        foreach (var item in collection)
        {
            if (item is null)
                continue;

            var itemElement = FormatObject(item, itemName);
            root.Add(itemElement);
        }

        return root;
    }

    private XElement FormatObject<T>(T obj, string elementName) where T : class
    {
        var element = new XElement(elementName);
        var properties = GetProperties(obj.GetType());

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);

            if (value is null)
                continue;

            var propElement = new XElement(ToCamelCase(prop.Name));

            // Handle different value types
            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (var item in enumerable)
                {
                    propElement.Add(new XElement("item", item?.ToString() ?? string.Empty));
                }
            }
            else if (IsComplexType(value.GetType()))
            {
                var complexElement = FormatObject(value, ToCamelCase(prop.Name));
                propElement = complexElement;
            }
            else
            {
                propElement.Value = value.ToString() ?? string.Empty;
            }

            element.Add(propElement);
        }

        return element;
    }

    private PropertyInfo[] GetProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
    }

    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive &&
               type != typeof(string) &&
               type != typeof(decimal) &&
               type != typeof(DateTime) &&
               type != typeof(TimeSpan) &&
               type != typeof(Guid);
    }

    private string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLower(str[0]) + str.Substring(1);
    }

    private string BuildXmlString(XElement root)
    {
        var doc = _includeDeclaration
            ? new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root)
            : new XDocument(root);

        return doc.ToString();
    }

    private string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
