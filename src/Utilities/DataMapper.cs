#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Generic data mapper for converting between entities and DTOs.
/// Provides simple property mapping with support for nested objects.
/// Useful for transforming domain models to/from API contracts.
/// </summary>
public interface IDataMapper
{
    TTarget Map<TSource, TTarget>(TSource source) where TTarget : class, new();
    List<TTarget> MapList<TSource, TTarget>(List<TSource> sources) where TTarget : class, new();
}

public sealed class DataMapper : IDataMapper {
    private readonly ILogger<DataMapper> _logger;
    private readonly Dictionary<string, PropertyInfo[]> _propertyCache;

    public DataMapper(ILogger<DataMapper> logger)
    {
        _logger = logger;
        _propertyCache = new Dictionary<string, PropertyInfo[]>();
    }

    /// <summary>
    /// Maps a source object to a target object.
    /// Copies properties with matching names and compatible types.
    /// </summary>
    public TTarget Map<TSource, TTarget>(TSource source) where TTarget : class, new()
    {
        try
        {
            if (source is null)
                return new TTarget();

            var target = new TTarget();
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            var sourceProperties = GetProperties(sourceType);
            var targetProperties = GetProperties(targetType);

            foreach (var sourceProperty in sourceProperties)
            {
                var targetProperty = targetProperties
                    .FirstOrDefault(p => p.Name.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase));

                if (targetProperty is not null && targetProperty.CanWrite && sourceProperty.CanRead)
                {
                    try
                    {
                        var value = sourceProperty.GetValue(source);

                        // Handle type conversion
                        if (value is not null && targetProperty.PropertyType != sourceProperty.PropertyType)
                        {
                            value = ConvertValue(value, targetProperty.PropertyType);
                        }

                        targetProperty.SetValue(target, value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            $"Failed to map property {sourceProperty.Name}: {ex.Message}");
                    }
                }
            }

            return target;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Mapping error from {typeof(TSource).Name} to {typeof(TTarget).Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Maps a list of source objects to target objects.
    /// </summary>
    public List<TTarget> MapList<TSource, TTarget>(List<TSource> sources) where TTarget : class, new()
    {
        try
        {
            if (sources is null || sources.Count == 0)
                return new List<TTarget>();

            return sources.Select(s => Map<TSource, TTarget>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("List mapping error: {Message}", ex.Message);
            throw;
        }
    }

    private PropertyInfo[] GetProperties(Type type)
    {
        var cacheKey = type.FullName!;

        if (!_propertyCache.TryGetValue(cacheKey, out var properties))
        {
            properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();

            _propertyCache[cacheKey] = properties;
        }

        return properties;
    }

    private object? ConvertValue(object value, Type targetType)
    {
        try
        {
            if (value is null)
                return null;

            if (targetType == typeof(string))
                return value.ToString();

            if (targetType == typeof(int))
                return Convert.ToInt32(value);

            if (targetType == typeof(long))
                return Convert.ToInt64(value);

            if (targetType == typeof(double))
                return Convert.ToDouble(value);

            if (targetType == typeof(bool))
                return Convert.ToBoolean(value);

            if (targetType == typeof(DateTime))
                return Convert.ToDateTime(value);

            if (targetType == typeof(Guid))
            {
                return value is Guid guid ? guid : Guid.Parse(value.ToString()!);
            }

            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Type conversion failed: {Message}", ex.Message);
            return null;
        }
    }
}

/// <summary>
/// Custom property mapping configurations.
/// </summary>
public sealed class MappingProfile {
    private readonly Dictionary<string, Func<object, object>> _customMappings;

    public MappingProfile()
    {
        _customMappings = new Dictionary<string, Func<object, object>>();
    }

    /// <summary>
    /// Adds a custom mapping function for a property.
    /// </summary>
    public void AddCustomMapping<TSource, TTarget>(
        string propertyName,
        Func<TSource, object> mappingFunc) where TSource : class
    {
        var key = $"{typeof(TSource).Name}.{propertyName}";
        _customMappings[key] = source => mappingFunc((TSource)source);
    }

    /// <summary>
    /// Gets a custom mapping function if available.
    /// </summary>
    public bool TryGetCustomMapping(string typeName, string propertyName, out Func<object, object>? mapping)
    {
        var key = $"{typeName}.{propertyName}";
        return _customMappings.TryGetValue(key, out mapping);
    }
}
