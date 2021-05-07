#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Buffers;
using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SqliteMultiTenant.Utilities;

/// <summary>
/// Advanced string manipulation utilities for common operations.
/// Provides hashing, truncation, case conversion, sanitization, and validation.
/// </summary>
public static class StringUtilities
{
    // Compiled once — ToSnakeCase is called on every schema-mapping round-trip.
    private static readonly Regex SnakeCaseRegex =
        new Regex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled);

    // Cached once; Path.GetInvalidFileNameChars() allocates a new array on each call.
    private static readonly FrozenSet<char> InvalidFileNameCharSet =
        Path.GetInvalidFileNameChars().ToFrozenSet();

    /// <summary>
    /// Computes SHA256 hash of the input string.
    /// Returns hexadecimal representation of hash.
    /// </summary>
    public static string ComputeSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(input.Length);
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var bytesEncoded = Encoding.UTF8.GetBytes(input, 0, input.Length, rentedBuffer, 0);
            Span<byte> hashBytes = stackalloc byte[32];
            SHA256.HashData(rentedBuffer.AsSpan(0, bytesEncoded), hashBytes);
            return Convert.ToHexString(hashBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// Computes MD5 hash of the input string.
    /// Returns hexadecimal representation of hash.
    /// </summary>
    public static string ComputeMd5Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(input.Length);
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var bytesEncoded = Encoding.UTF8.GetBytes(input, 0, input.Length, rentedBuffer, 0);
            Span<byte> hashBytes = stackalloc byte[16];
            MD5.HashData(rentedBuffer.AsSpan(0, bytesEncoded), hashBytes);
            return Convert.ToHexString(hashBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// Truncates string to maximum length and adds ellipsis if truncated.
    /// </summary>
    public static string TruncateWithEllipsis(string input, int maxLength = 100)
    {
        if (maxLength < 3)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Maximum length must be at least 3 to accommodate ellipsis.");

        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        // Fix: Added bounds checking for maxLength to prevent index out of range exception when < 3
        return input[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Converts string to title case (capitalize each word).
    /// </summary>
    public static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var textInfo = new CultureInfo("en-US", false).TextInfo;
        return textInfo.ToTitleCase(input.ToLower());
    }

    /// <summary>
    /// Converts string from camelCase to snake_case.
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return SnakeCaseRegex.Replace(input, "$1_$2").ToLower();
    }

    /// <summary>
    /// Converts string from snake_case to camelCase.
    /// </summary>
    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var parts = input.Split('_');
        var result = new StringBuilder(parts[0].ToLower());

        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                result.Append(char.ToUpper(parts[i][0]) + parts[i][1..]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes all whitespace from string.
    /// </summary>
    public static string RemoveWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return Regex.Replace(input, @"\s+", string.Empty);
    }

    /// <summary>
    /// Sanitizes string for safe use in file paths.
    /// Removes or replaces invalid file name characters.
    /// </summary>
    public static string SanitizeForFilePath(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Single pass over the input with a pooled char buffer avoids the O(k)
        // chained string.Replace calls (one allocation per invalid-char category).
        var buffer = ArrayPool<char>.Shared.Rent(input.Length);
        try
        {
            int writeIdx = 0;
            foreach (var c in input.AsSpan())
            {
                if (c == ' ')
                    buffer[writeIdx++] = '_';
                else if (!InvalidFileNameCharSet.Contains(c))
                    buffer[writeIdx++] = c;
            }
            return new string(buffer, 0, writeIdx);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Sanitizes string for safe HTML output (prevents XSS).
    /// </summary>
    public static string SanitizeForHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Checks if string is valid email format.
    /// </summary>
    public static bool IsValidEmail(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(input);
            return addr.Address == input;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if string is valid URL format.
    /// </summary>
    public static bool IsValidUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Uri.TryCreate(input, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Checks if string is valid GUID format.
    /// </summary>
    public static bool IsValidGuid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return Guid.TryParse(input, out _);
    }

    /// <summary>
    /// Generates random alphanumeric string of specified length.
    /// </summary>
    public static string GenerateRandomString(int length = 16)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();

        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// Repeats string specified number of times.
    /// </summary>
    public static string Repeat(string input, int count)
    {
        if (count <= 0 || string.IsNullOrEmpty(input))
            return string.Empty;

        return string.Concat(Enumerable.Repeat(input, count));
    }

    /// <summary>
    /// Splits string while preserving quoted sections.
    /// </summary>
    public static IEnumerable<string> SplitPreservingQuotes(string input, char delimiter = ',')
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input), "Input string cannot be null.");

        // Fix: Added null check to prevent NullReferenceException during string enumeration
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        bool inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentPart.Append(c);
            }
            else if (c == delimiter && !inQuotes)
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }
            else
            {
                currentPart.Append(c);
            }
        }

        if (currentPart.Length > 0)
            parts.Add(currentPart.ToString().Trim());

        return parts;
    }

    /// <summary>
    /// Gets the similarity ratio between two strings (0-1).
    /// Uses Levenshtein distance algorithm.
    /// </summary>
    public static double GetStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0;

        int distance = LevenshteinDistance(str1, str2);
        int maxLength = Math.Max(str1.Length, str2.Length);

        return 1.0 - (double)distance / maxLength;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        int len1 = s1.Length;
        int len2 = s2.Length;
        int[,] distances = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
            distances[i, 0] = i;

        for (int j = 0; j <= len2; j++)
            distances[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[len1, len2];
    }
}
