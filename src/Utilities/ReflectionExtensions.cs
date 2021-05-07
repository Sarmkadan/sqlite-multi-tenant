#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for reflection operations.
/// Simplifies type inspection and dynamic property access.
/// Used for formatters, serialization, and metadata extraction.
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Gets all public properties of a type with caching for performance.
    /// Avoids repeated reflection calls by caching metadata.
    /// </summary>
    private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new();

    public static PropertyInfo[] GetPublicProperties(this Type type)
    {
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
    public static object GetPropertyValue(this object obj, string propertyName)
    {
        if (obj is null || string.IsNullOrWhiteSpace(propertyName))
            return null;

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets property value using reflection.
    /// Returns false if property doesn't exist or can't be set.
    /// </summary>
    public static bool SetPropertyValue(this object obj, string propertyName, object value)
    {
        if (obj is null || string.IsNullOrWhiteSpace(propertyName))
            return false;

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
    public static bool IsCollection(this Type type)
    {
        if (type == typeof(string))
            return false;

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Gets the generic type argument for a collection.
    /// Example: List<int> returns int.
    /// </summary>
    public static Type GetCollectionElementType(this Type type)
    {
        if (!type.IsCollection())
            return null;

        var genericArgs = type.GetGenericArguments();
        return genericArgs.Length > 0 ? genericArgs[0] : typeof(object);
    }

    /// <summary>
    /// Checks if type is a nullable value type.
    /// Example: int? returns true, int returns false.
    /// </summary>
    public static bool IsNullable(this Type type)
    {
        return Nullable.GetUnderlyingType(type) is not null;
    }

    /// <summary>
    /// Gets the underlying type for nullable types.
    /// Example: int? returns int, int returns int.
    /// </summary>
    public static Type GetUnderlyingType(this Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// Checks if type is a simple scalar type (not collection or complex object).
    /// </summary>
    public static bool IsScalarType(this Type type)
    {
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
    public static MethodInfo[] GetMethodsByName(this Type type, string methodName)
    {
        return type.GetMethods(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Creates instance of type with parameterless constructor.
    /// Returns null if instantiation fails.
    /// </summary>
    public static object CreateInstance(this Type type)
    {
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
    public static bool HasAttribute<T>(this Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>() is not null;
    }

    /// <summary>
    /// Gets custom attribute from type.
    /// </summary>
    public static T GetAttribute<T>(this Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>();
    }

    /// <summary>
    /// Copies properties from one object to another.
    /// Only copies matching property names with same types.
    /// </summary>
    public static void CopyPropertiesTo<T>(this object source, T destination) where T : class
    {
        if (source is null || destination is null)
            return;

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
