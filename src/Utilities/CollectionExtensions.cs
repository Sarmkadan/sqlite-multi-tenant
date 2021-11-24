#nullable enable

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
    /// <param name="list">The list to access. Cannot be null.</param>
    /// <param name="index">The zero-based index. Must be non-negative.</param>
    /// <param name="defaultValue">The value to return if index is out of bounds.</param>
    /// <returns>The element at the specified index, or <paramref name="defaultValue"/> if index is out of bounds.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is null.</exception>
    public static T SafeGet<T>(this IList<T> list, int index, T defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    /// <summary>
    /// Safely gets first element or returns default value if collection is empty.
    /// More efficient than LINQ FirstOrDefault() for small lists.
    /// </summary>
    /// <param name="list">The list to access. Can be null.</param>
    /// <param name="defaultValue">The value to return if collection is empty.</param>
    /// <returns>The first element, or <paramref name="defaultValue"/> if collection is empty.</returns>
    public static T SafeFirst<T>(this IList<T>? list, T defaultValue = default)
    {
        return list?.Count > 0 ? list[0] : defaultValue;
    }

    /// <summary>
    /// Safely gets last element or returns default value if collection is empty.
    /// </summary>
    /// <param name="list">The list to access. Can be null.</param>
    /// <param name="defaultValue">The value to return if collection is empty.</param>
    /// <returns>The last element, or <paramref name="defaultValue"/> if collection is empty.</returns>
    public static T SafeLast<T>(this IList<T>? list, T defaultValue = default)
    {
        return list?.Count > 0 ? list[list.Count - 1] : defaultValue;
    }

    /// <summary>
    /// Chunks a collection into smaller batches for bulk operations.
    /// Useful for batch database inserts or API bulk calls.
    /// Example: items.Chunk(100) creates lists of 100 items each.
    /// </summary>
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <param name="chunkSize">The size of each chunk. Must be positive.</param>
    /// <returns>A list of chunks, each containing up to <paramref name="chunkSize"/> items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="chunkSize"/> is not positive.</exception>
    public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(chunkSize, 0);

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
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <returns>A filtered sequence containing only non-null elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(x => x is not null);
    }

    /// <summary>
    /// Executes action for each element (side-effect oriented).
    /// Useful for logging or operations during iteration.
    /// </summary>
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <param name="action">The action to execute for each element. Cannot be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="action"/> is null.</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Executes action for each element with index.
    /// </summary>
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <param name="action">The action to execute for each element with its index. Cannot be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="action"/> is null.</exception>
    public static void ForEachWithIndex<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

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
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <param name="keySelector">The function to extract the key for comparison. Cannot be null.</param>
    /// <returns>A sequence of distinct elements based on the key selector.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    /// <typeparam name="T">The type of elements in the source collection.</typeparam>
    /// <typeparam name="TKey">The type of key used for comparison.</typeparam>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

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
    /// <param name="source">The source collection. Can be null.</param>
    /// <returns>True if the collection has one or more elements; otherwise, false.</returns>
    public static bool HasElements<T>(this IEnumerable<T>? source)
    {
        return source?.Any() ?? false;
    }

    /// <summary>
    /// Randomly shuffles collection elements.
    /// Uses Fisher-Yates algorithm for unbiased shuffling.
    /// </summary>
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <returns>A new list containing all elements in random order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static List<T> Shuffle<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

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
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <param name="dateSelector">The function to extract the date component. Cannot be null.</param>
    /// <returns>A dictionary mapping dates to lists of items for that date.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="dateSelector"/> is null.</exception>
    /// <typeparam name="T">The type of elements in the source collection.</typeparam>
    public static Dictionary<DateTime, List<T>> GroupByDate<T>(
        this IEnumerable<T> source,
        Func<T, DateTime> dateSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(dateSelector);

        return source
            .GroupBy(x => dateSelector(x).Date)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Safely casts collection to list, returns empty list if null.
    /// </summary>
    /// <param name="source">The source collection. Can be null.</param>
    /// <returns>A list containing all elements, or an empty list if source is null.</returns>
    public static List<T> ToListSafe<T>(this IEnumerable<T>? source)
    {
        return source?.ToList() ?? new List<T>();
    }

    /// <summary>
    /// Checks if collection contains any duplicates.
    /// </summary>
    /// <param name="source">The source collection. Cannot be null.</param>
    /// <returns>True if the collection contains duplicates; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static bool HasDuplicates<T>(this IEnumerable<T> source) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var seen = new HashSet<T>();
        return source.Any(item => !seen.Add(item));
    }

    /// <summary>
    /// Intersects two collections by key selector (e.g., find common tenant IDs).
    /// </summary>
    /// <param name="first">The first collection. Cannot be null.</param>
    /// <param name="second">The second collection. Cannot be null.</param>
    /// <param name="keySelector">The function to extract the key for comparison. Cannot be null.</param>
    /// <returns>A sequence containing elements from <paramref name="first"/> that have matching keys in <paramref name="second"/>.</returns>
    /// <exception cref="ArgumentNullException">Any of the parameters is null.</exception>
    /// <typeparam name="T">The type of elements in the collections.</typeparam>
    /// <typeparam name="TKey">The type of key used for comparison.</typeparam>
    public static IEnumerable<T> IntersectBy<T, TKey>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(keySelector);

        var secondKeys = second.Select(keySelector).ToHashSet();
        return first.Where(x => secondKeys.Contains(keySelector(x)));
    }
}