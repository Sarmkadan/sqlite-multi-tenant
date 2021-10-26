# CollectionExtensions

`CollectionExtensions` provides a set of static, defensive, and utility methods for working with collections, lists, and enumerables in the `sqlite-multi-tenant` project. It is designed to reduce boilerplate null-checking, offer safe access to elements, and simplify common partitioning, filtering, and transformation tasks on in-memory sequences.

## API

### SafeGet<T>
```csharp
public static T SafeGet<T>(this IList<T> list, int index)
```
Returns the element at the specified `index` without throwing an `ArgumentOutOfRangeException`. If the `list` is `null` or the `index` is outside the valid range, the `default` value for `T` is returned.

**Parameters:**
- `list` — The source `IList<T>`. Can be `null`.
- `index` — The zero-based position to retrieve.

**Returns:** The element at `index`, or `default(T)` if the list is `null` or the index is invalid.

**Throws:** Nothing. All invalid states are handled gracefully.

---

### SafeFirst<T>
```csharp
public static T SafeFirst<T>(this IEnumerable<T> source)
```
Returns the first element of the sequence, or `default(T)` if the sequence is `null` or empty.

**Parameters:**
- `source` — The source `IEnumerable<T>`. Can be `null`.

**Returns:** The first element, or `default(T)`.

**Throws:** Nothing.

---

### SafeLast<T>
```csharp
public static T SafeLast<T>(this IEnumerable<T> source)
```
Returns the last element of the sequence, or `default(T)` if the sequence is `null` or empty. For non-indexed enumerables this may require full enumeration.

**Parameters:**
- `source` — The source `IEnumerable<T>`. Can be `null`.

**Returns:** The last element, or `default(T)`.

**Throws:** Nothing.

---

### ChunkBy<T>
```csharp
public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
```
Splits the source sequence into a list of chunks, each being a `List<T>` of at most `chunkSize` elements. The final chunk may be smaller if the total count is not evenly divisible.

**Parameters:**
- `source` — The sequence to partition.
- `chunkSize` — The maximum size of each chunk. Must be greater than zero.

**Returns:** A `List<List<T>>` where each inner list represents a chunk.

**Throws:**
- `ArgumentNullException` if `source` is `null`.
- `ArgumentOutOfRangeException` if `chunkSize` is less than 1.

---

### WhereNotNull<T>
```csharp
public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
```
Filters out `null` values from a sequence of nullable reference types, returning only non-null instances. The result is typed as `IEnumerable<T>` (non-nullable).

**Parameters:**
- `source` — The sequence potentially containing `null` entries.

**Returns:** An `IEnumerable<T>` containing only the non-null elements.

**Throws:** `ArgumentNullException` if `source` is `null`.

---

### ForEach<T>
```csharp
public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
```
Executes the specified `action` on each element of the sequence. Provided as a fluent alternative to `foreach` loops.

**Parameters:**
- `source` — The sequence to iterate.
- `action` — The delegate to invoke per element.

**Throws:**
- `ArgumentNullException` if `source` or `action` is `null`.

---

### ForEachWithIndex<T>
```csharp
public static void ForEachWithIndex<T>(this IEnumerable<T> source, Action<T, int> action)
```
Executes the specified `action` on each element, passing both the element and its zero-based index.

**Parameters:**
- `source` — The sequence to iterate.
- `action` — The delegate receiving the element and its index.

**Throws:**
- `ArgumentNullException` if `source` or `action` is `null`.

---

### DistinctBy<T, TKey>
```csharp
public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
```
Returns distinct elements based on a projected key. The first occurrence of each key is retained; subsequent duplicates are omitted.

**Parameters:**
- `source` — The sequence to filter.
- `keySelector` — A function to extract the comparison key from each element.

**Returns:** An `IEnumerable<T>` with duplicates removed by key.

**Throws:**
- `ArgumentNullException` if `source` or `keySelector` is `null`.

---

### HasElements<T>
```csharp
public static bool HasElements<T>(this IEnumerable<T> source)
```
Determines whether the sequence contains at least one element. Handles `null` sources safely.

**Parameters:**
- `source` — The sequence to test. Can be `null`.

**Returns:** `true` if the sequence is not `null` and contains any elements; otherwise `false`.

**Throws:** Nothing.

---

### Shuffle<T>
```csharp
public static List<T> Shuffle<T>(this IEnumerable<T> source)
```
Returns a new `List<T>` containing all elements of the source in a randomised order. Uses a cryptographically non-strong random generator suitable for general-purpose shuffling.

**Parameters:**
- `source` — The sequence to shuffle.

**Returns:** A new `List<T>` with elements randomly reordered.

**Throws:** `ArgumentNullException` if `source` is `null`.

---

### GroupByDate<T>
```csharp
public static Dictionary<DateTime, List<T>> GroupByDate<T>(
    this IEnumerable<T> source,
    Func<T, DateTime> dateSelector)
```
Groups elements into a dictionary keyed by the *date* component (time is truncated) of the `DateTime` returned by the selector. Each key maps to a `List<T>` of elements sharing that date.

**Parameters:**
- `source` — The sequence to group.
- `dateSelector` — A function returning a `DateTime` for each element.

**Returns:** A `Dictionary<DateTime, List<T>>` keyed by date-only values.

**Throws:**
- `ArgumentNullException` if `source` or `dateSelector` is `null`.

---

### ToListSafe<T>
```csharp
public static List<T> ToListSafe<T>(this IEnumerable<T> source)
```
Converts the sequence to a `List<T>`. If the source is `null`, an empty `List<T>` is returned instead of throwing.

**Parameters:**
- `source` — The sequence to materialise. Can be `null`.

**Returns:** A `List<T>` containing the elements, or an empty list.

**Throws:** Nothing.

---

### HasDuplicates<T>
```csharp
public static bool HasDuplicates<T>(this IEnumerable<T> source)
```
Checks whether the sequence contains any duplicate elements using the default equality comparer.

**Parameters:**
- `source` — The sequence to inspect.

**Returns:** `true` if any element appears more than once; otherwise `false`.

**Throws:** `ArgumentNullException` if `source` is `null`.

---

### IntersectBy<T, TKey>
```csharp
public static IEnumerable<T> IntersectBy<T, TKey>(
    this IEnumerable<T> first,
    IEnumerable<T> second,
    Func<T, TKey> keySelector)
```
Returns the set intersection of two sequences based on a projected key. Elements from `first` are yielded when their key also appears in `second`.

**Parameters:**
- `first` — The primary sequence.
- `second` — The sequence whose keys define the intersection.
- `keySelector` — A function to extract the comparison key.

**Returns:** An `IEnumerable<T>` of elements from `first` whose keys exist in `second`.

**Throws:**
- `ArgumentNullException` if any argument is `null`.

---

## Usage

### Example 1: Safe retrieval and batch processing
```csharp
var records = tenantService.GetLogEntries(); // may return null
if (records.HasElements())
{
    var chunks = records.ChunkBy(100);
    chunks.ForEach(chunk =>
    {
        bulkProcessor.Insert(chunk);
    });
}
else
{
    var fallback = fallbackSource.ToListSafe();
    fallback.ForEachWithIndex((item, i) =>
    {
        Console.WriteLine($"Fallback {i}: {item}");
    });
}
```

### Example 2: Deduplication and date-based grouping
```csharp
var transactions = paymentGateway.FetchPending();
var unique = transactions.DistinctBy(tx => tx.ReferenceId);
if (unique.HasDuplicates(tx => tx.ReferenceId))
{
    // Should not happen after DistinctBy, but defensive check
    logger.Warn("Duplicate references remain");
}

var byDate = unique.GroupByDate(tx => tx.CreatedUtc);
foreach (var day in byDate)
{
    var shuffledBatch = day.Value.Shuffle();
    processor.Distribute(shuffledBatch);
}
```

## Notes

- **Null handling:** Methods prefixed with `Safe` (`SafeGet`, `SafeFirst`, `SafeLast`, `ToListSafe`) and `HasElements` treat a `null` source as an empty collection rather than throwing. All other methods throw `ArgumentNullException` when the primary source is `null`, following the standard .NET convention of failing fast for unexpected nulls.
- **Deferred execution:** Methods returning `IEnumerable<T>` (`WhereNotNull`, `DistinctBy`, `IntersectBy`) use deferred execution. The input sequence is not enumerated until the result is iterated. Validate arguments eagerly, but filtering logic is lazy.
- **`GroupByDate` truncation:** The `DateTime` key retains only the date portion; the time component is zeroed. Elements with the same calendar date but different times will be grouped together.
- **`Shuffle` randomness:** The shuffle uses `System.Random` and is not intended for cryptographic purposes. Do not rely on it for security-sensitive randomisation.
- **Thread safety:** All methods are static and operate on their own local state or enumerator instances. They do not mutate the source collection. Thread safety depends entirely on the thread safety of the underlying collection passed in. If the source is being modified concurrently during enumeration, standard enumerator invalidation rules apply.
