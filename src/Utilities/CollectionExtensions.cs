// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Extension methods for collections commonly used in filtering, sorting, and batching operations.
/// Provides functional programming patterns while avoiding LINQ performance pitfalls.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Safely gets an element at index or returns default value if out of bounds.
    /// Prevents IndexOutOfRangeException when accessing collections with unknown size.
    /// </summary>
    public static T SafeGet<T>(this IList<T> list, int index, T defaultValue = default)
    {
        if (list == null || index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    /// <summary>
    /// Safely gets first element or returns default value if collection is empty.
    /// More efficient than LINQ FirstOrDefault() for small lists.
    /// </summary>
    public static T SafeFirst<T>(this IList<T> list, T defaultValue = default)
    {
        return list?.Count > 0 ? list[0] : defaultValue;
    }

    /// <summary>
    /// Safely gets last element or returns default value if collection is empty.
    /// </summary>
    public static T SafeLast<T>(this IList<T> list, T defaultValue = default)
    {
        return list?.Count > 0 ? list[list.Count - 1] : defaultValue;
    }

    /// <summary>
    /// Chunks a collection into smaller batches for bulk operations.
    /// Useful for batch database inserts or API bulk calls.
    /// Example: items.Chunk(100) creates lists of 100 items each.
    /// </summary>
    public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive");

        var chunks = new List<List<T>>();
        var currentChunk = new List<T>();

        foreach (var item in source)
        {
            currentChunk.Add(item);

            if (currentChunk.Count == chunkSize)
            {
                chunks.Add(currentChunk);
                currentChunk = new List<T>();
            }
        }

        if (currentChunk.Count > 0)
            chunks.Add(currentChunk);

        return chunks;
    }

    /// <summary>
    /// Filters out null elements from collection.
    /// More readable than LINQ Where(x => x != null).
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
    {
        return source.Where(x => x != null);
    }

    /// <summary>
    /// Executes action for each element (side-effect oriented).
    /// Useful for logging or operations during iteration.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Executes action for each element with index.
    /// </summary>
    public static void ForEachWithIndex<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        int index = 0;
        foreach (var item in source)
        {
            action(item, index);
            index++;
        }
    }

    /// <summary>
    /// Returns distinct elements by a specified key selector.
    /// More efficient than GroupBy for simple deduplication.
    /// Example: backups.DistinctBy(b => b.DatabaseId)
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seen.Add(key))
                yield return item;
        }
    }

    /// <summary>
    /// Returns true if collection has any elements (same as Any() but named more clearly).
    /// </summary>
    public static bool HasElements<T>(this IEnumerable<T> source)
    {
        return source?.Any() ?? false;
    }

    /// <summary>
    /// Randomly shuffles collection elements.
    /// Uses Fisher-Yates algorithm for unbiased shuffling.
    /// </summary>
    public static List<T> Shuffle<T>(this IEnumerable<T> source)
    {
        var list = source.ToList();
        var random = new Random();

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }

        return list;
    }

    /// <summary>
    /// Groups items by date (ignoring time component).
    /// Useful for daily backup aggregation and reporting.
    /// </summary>
    public static Dictionary<DateTime, List<T>> GroupByDate<T>(
        this IEnumerable<T> source,
        Func<T, DateTime> dateSelector)
    {
        return source
            .GroupBy(x => dateSelector(x).Date)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Safely casts collection to list, returns empty list if null.
    /// </summary>
    public static List<T> ToListSafe<T>(this IEnumerable<T> source)
    {
        return source?.ToList() ?? new List<T>();
    }

    /// <summary>
    /// Checks if collection contains any duplicates.
    /// </summary>
    public static bool HasDuplicates<T>(this IEnumerable<T> source) where T : notnull
    {
        var seen = new HashSet<T>();
        return source.Any(item => !seen.Add(item));
    }

    /// <summary>
    /// Intersects two collections by key selector (e.g., find common tenant IDs).
    /// </summary>
    public static IEnumerable<T> IntersectBy<T, TKey>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        var secondKeys = second.Select(keySelector).ToHashSet();
        return first.Where(x => secondKeys.Contains(keySelector(x)));
    }
}
