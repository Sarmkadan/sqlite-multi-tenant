#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Reflection;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for reflection operations.
/// Provides utilities for type inspection, dynamic property access, and metadata extraction.
/// Used for formatters, serialization, and runtime type analysis.
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Gets all public properties of a type with caching for performance.
    /// Avoids repeated reflection calls by caching metadata in a thread-safe manner.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>
    /// Gets all public instance properties of the specified type.
    /// Results are cached in a thread-safe dictionary to improve performance.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Array of public instance properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static PropertyInfo[] GetPublicProperties(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (PropertyCache.TryGetValue(type, out var cached))
            return cached;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        PropertyCache[type] = properties;
        return properties;
    }

    /// <summary>
    /// Gets property value from object using reflection.
    /// Returns null if property doesn't exist.
    /// </summary>
    /// <param name="obj">The object to inspect.</param>
    /// <param name="propertyName">Name of the property to get.</param>
    /// <returns>The property value or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null or whitespace.</exception>
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets property value using reflection.
    /// Returns false if property doesn't exist or can't be set.
    /// </summary>
    /// <param name="obj">The object whose property to set.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>True if property was set successfully; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> or <paramref name="propertyName"/> is null.</exception>
    public static bool SetPropertyValue(this object obj, string propertyName, object? value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property is null || !property.CanWrite)
            return false;

        try
        {
            property.SetValue(obj, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if type is a collection (IEnumerable but not string).
    /// Useful for determining if object should be enumerated.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a collection; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static bool IsCollection(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets the generic type argument for a collection.
    /// Example: List&lt;int&gt; returns int.
    /// </summary>
    /// <param name="type">The collection type.</param>
    /// <returns>The element type of the collection, or typeof(object) if no generic arguments exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static Type? GetCollectionElementType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsCollection())
            return null;

        var genericArgs = type.GetGenericArguments();
        return genericArgs.Length > 0 ? genericArgs[0] : typeof(object);
    }

    /// <summary>
    /// Checks if type is a nullable value type.
    /// Example: int? returns true, int returns false.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is nullable; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static bool IsNullable(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Nullable.GetUnderlyingType(type) is not null;
    }

    /// <summary>
    /// Gets the underlying type for nullable types.
    /// Example: int? returns int, int returns int.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The underlying type if nullable; otherwise the original type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static Type GetUnderlyingType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// Checks if type is a simple scalar type (not collection or complex object).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a scalar; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static bool IsScalarType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type.IsEnum;
    }

    /// <summary>
    /// Gets all methods with specified name (ignoring case).
    /// Useful for finding overloaded methods by name.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="methodName">Name of the method to find.</param>
    /// <returns>Array of matching methods.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> or <paramref name="methodName"/> is null.</exception>
    public static MethodInfo[] GetMethodsByName(this Type type, string methodName)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        return type.GetMethods(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Creates instance of type with parameterless constructor.
    /// Returns null if instantiation fails.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <returns>New instance or null if creation failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static object? CreateInstance(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if type has attribute of specified type.
    /// </summary>
    /// <typeparam name="T">The attribute type to check for.</typeparam>
    /// <param name="type">The type to inspect.</param>
    /// <returns>True if the attribute exists; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static bool HasAttribute<T>(this Type type) where T : Attribute
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetCustomAttribute<T>() is not null;
    }

    /// <summary>
    /// Gets custom attribute from type.
    /// </summary>
    /// <typeparam name="T">The attribute type to retrieve.</typeparam>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The attribute instance or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
    public static T? GetAttribute<T>(this Type type) where T : Attribute
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetCustomAttribute<T>();
    }

    /// <summary>
    /// Copies properties from one object to another.
    /// Only copies matching property names with same types.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <param name="destination">The destination object.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
    public static void CopyPropertiesTo<T>(this object source, T destination) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var sourceType = source.GetType();
        var destType = destination.GetType();

        var sourceProperties = sourceType.GetPublicProperties();

        foreach (var sourceProp in sourceProperties)
        {
            var destProp = destType.GetProperty(sourceProp.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (destProp is not null && destProp.CanWrite && sourceProp.CanRead)
            {
                try
                {
                    var value = sourceProp.GetValue(source);
                    destProp.SetValue(destination, value);
                }
                catch
                {
                    // Skip properties that can't be copied
                }
            }
        }
    }
}