# TimeSpanExtensions

`TimeSpanExtensions` provides formatting and comparison helpers for `TimeSpan` values in the `SqliteMultiTenant.Utilities` namespace.

## ToHumanReadable

```csharp
public static string ToHumanReadable(this TimeSpan ts)
```

Returns a space-separated representation made from the non-zero day, hour, minute, and second components. A zero value is returned as `"0s"`. Milliseconds are included only when every larger component is zero.

| Input | Output |
| :--- | :--- |
| `TimeSpan.Zero` | `"0s"` |
| `new TimeSpan(2, 5, 7, 9)` | `"2d 5h 7m 9s"` |
| `TimeSpan.FromMinutes(65)` | `"1h 5m"` |
| `TimeSpan.FromMilliseconds(250)` | `"250ms"` |
| `TimeSpan.FromSeconds(1.25)` | `"1s"` |

The method uses `TimeSpan` component properties rather than total units. Consequently, fractional milliseconds below one millisecond produce an empty string, and milliseconds are omitted whenever a day, hour, minute, or second component is present.

## ToCompact

```csharp
public static string ToCompact(this TimeSpan ts)
```

Formats the value with the culture-invariant custom format `hh:mm:ss`. Each field is zero-padded, and fractional seconds are omitted.

| Input | Output |
| :--- | :--- |
| `TimeSpan.Zero` | `"00:00:00"` |
| `new TimeSpan(0, 2, 5, 9)` | `"02:05:09"` |
| `new TimeSpan(1, 2, 3, 4)` | `"02:03:04"` |
| `TimeSpan.FromMilliseconds(1500)` | `"00:00:01"` |

Because the format contains no day field, hours represent only the hour component within a day. For example, 26 hours is formatted as `"02:00:00"`, not `"26:00:00"`.

## IsWithin

```csharp
public static bool IsWithin(this TimeSpan ts, TimeSpan tolerance)
```

Returns the result of the direct comparison `ts <= tolerance`. Equality is included.

| Value | Tolerance | Output |
| :--- | :--- | :--- |
| `TimeSpan.FromSeconds(5)` | `TimeSpan.FromSeconds(10)` | `true` |
| `TimeSpan.FromSeconds(10)` | `TimeSpan.FromSeconds(10)` | `true` |
| `TimeSpan.FromSeconds(11)` | `TimeSpan.FromSeconds(10)` | `false` |
| `TimeSpan.FromSeconds(-20)` | `TimeSpan.FromSeconds(10)` | `true` |

`IsWithin` does not calculate an absolute difference and does not validate that either value is non-negative. A negative value therefore compares as within a positive tolerance.

## Usage

```csharp
using SqliteMultiTenant.Utilities;

TimeSpan elapsed = new TimeSpan(0, 2, 5, 9);

string readable = elapsed.ToHumanReadable(); // "2h 5m 9s"
string compact = elapsed.ToCompact();         // "02:05:09"
bool acceptable = elapsed.IsWithin(TimeSpan.FromHours(3)); // true
```

All three methods are static and stateless. The formatting methods do not round their input values.
