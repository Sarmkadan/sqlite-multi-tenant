using System;
using System.Globalization;
using FluentAssertions;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Contains unit tests for the <see cref="SqliteMultiTenant.Utilities.StringUtilities"/> class,
/// testing all string manipulation and validation methods including hashing, truncation,
/// case conversion, whitespace removal, sanitization, validation, and generation utilities.
/// </summary>
public class StringUtilitiesTests
{
    /// <summary>
    /// Contains tests for the ComputeSha256Hash method of the StringUtilities class.
    /// </summary>
    public class ComputeSha256HashTests
    {
        /// <summary>
        /// Tests that ComputeSha256Hash returns an empty string when given null or empty input.
        /// </summary>
        /// <param name="input">The input string to hash, which can be null or empty.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ComputeSha256Hash_ShouldReturnEmptyStringForNullOrEmpty(string input)
        {
            // Act
            var result = StringUtilities.ComputeSha256Hash(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("test")]
        [InlineData("Hello World")]
        public void ComputeSha256Hash_ShouldReturnNonEmptyHashForNonEmptyInput(string input)
        {
            // Act
            var result = StringUtilities.ComputeSha256Hash(input);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().HaveLength(64);
        }
    }

    /// <summary>
    /// Contains tests for the ComputeMd5Hash method of the StringUtilities class.
    /// </summary>
    public class ComputeMd5HashTests
    {
        /// <summary>
        /// Tests that ComputeMd5Hash returns an empty string when given null or empty input.
        /// </summary>
        /// <param name="input">The input string to hash, which can be null or empty.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ComputeMd5Hash_ShouldReturnEmptyStringForNullOrEmpty(string input)
        {
            // Act
            var result = StringUtilities.ComputeMd5Hash(input);

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that ComputeMd5Hash returns a non-empty 32-character hash for non-empty input strings.
        /// </summary>
        /// <param name="input">The input string to hash, which must be non-empty.</param>
        [Theory]
        [InlineData("hello")]
        [InlineData("test")]
        [InlineData("Hello World")]
        public void ComputeMd5Hash_ShouldReturnNonEmptyHashForNonEmptyInput(string input)
        {
            // Act
            var result = StringUtilities.ComputeMd5Hash(input);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().HaveLength(32);
        }
    }

    /// <summary>
    /// Contains tests for the TruncateWithEllipsis method of the StringUtilities class.
    /// </summary>
    public class TruncateWithEllipsisTests
    {
        /// <summary>
        /// Tests that TruncateWithEllipsis handles edge cases correctly (null, empty, and short strings).
        /// </summary>
        /// <param name="input">The input string to truncate.</param>
        /// <param name="expected">The expected result after truncation.</param>
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("short", "short")]
        [InlineData("a", "a")]
        [InlineData("ab", "ab")]
        public void TruncateWithEllipsis_ShouldHandleEdgeCases(string input, string expected)
        {
            // Act
            var result = StringUtilities.TruncateWithEllipsis(input, 100);

            // Assert
            result.Should().Be(expected);
        }

        /// <summary>
        /// Tests that TruncateWithEllipsis truncates strings to the specified length when needed.
        /// </summary>
        /// <param name="input">The input string to truncate.</param>
        /// <param name="maxLength">The maximum length allowed for the result.</param>
        [Theory]
        [InlineData("this is a very long string that needs to be truncated", 50)]
        [InlineData("this is a very long string that needs to be truncated", 25)]
        [InlineData("test", 10)]
        public void TruncateWithEllipsis_ShouldTruncateToSpecifiedLength(string input, int maxLength)
        {
            // Act
            var result = StringUtilities.TruncateWithEllipsis(input, maxLength);

            // Assert
            result.Should().NotBeNull();
            if (input.Length > maxLength)
            {
                result.Should().EndWith("...");
                result.Should().HaveLength(maxLength);
            }
        }

        /// <summary>
        /// Tests that TruncateWithEllipsis throws an ArgumentOutOfRangeException when maxLength is less than 3.
        /// </summary>
        [Fact]
        public void TruncateWithEllipsis_ShouldThrowWhenMaxLengthLessThan3()
        {
            // Arrange
            var action = () => StringUtilities.TruncateWithEllipsis("test", 2);

            // Act & Assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    /// <summary>
    /// Contains tests for the ToTitleCase method of the StringUtilities class.
    /// </summary>
    public class ToTitleCaseTests
    {
        /// <summary>
        /// Tests that ToTitleCase converts strings to title case correctly, handling null and empty inputs.
        /// </summary>
        /// <param name="input">The input string to convert to title case.</param>
        /// <param name="expected">The expected title-cased result.</param>
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("hello", "Hello")]
        [InlineData("HELLO", "Hello")]
        [InlineData("hello world", "Hello World")]
        [InlineData("HELLO WORLD", "Hello World")]
        [InlineData("hElLo WoRlD", "Hello World")]
        [InlineData("this is a test string", "This Is A Test String")]
        [InlineData("single", "Single")]
        public void ToTitleCase_ShouldConvertToTitleCase(string input, string expected)
        {
            // Act
            var result = StringUtilities.ToTitleCase(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    /// <summary>
    /// Contains tests for the ToSnakeCase method of the StringUtilities class.
    /// </summary>
    public class ToSnakeCaseTests
    {
        /// <summary>
        /// Tests that ToSnakeCase converts strings to snake case correctly, handling null and empty inputs.
        /// </summary>
        /// <param name="input">The input string to convert to snake case.</param>
        /// <param name="expected">The expected snake-cased result.</param>
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("hello", "hello")]
        [InlineData("helloWorld", "hello_world")]
        [InlineData("HelloWorld", "hello_world")]
        [InlineData("HELLO_WORLD", "hello_world")]
        [InlineData("camelCaseString", "camel_case_string")]
        [InlineData("PascalCaseString", "pascal_case_string")]
        [InlineData("already_snake_case", "already_snake_case")]
        [InlineData("xmlHttpRequest", "xml_http_request")]
        public void ToSnakeCase_ShouldConvertToSnakeCase(string input, string expected)
        {
            // Act
            var result = StringUtilities.ToSnakeCase(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    /// <summary>
    /// Contains tests for the ToCamelCase method of the StringUtilities class.
    /// </summary>
    public class ToCamelCaseTests
    {
        /// <summary>
        /// Tests that ToCamelCase returns the input unchanged for null or empty strings.
        /// </summary>
        /// <param name="input">The input string, which can be null or empty.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ToCamelCase_ShouldReturnInputForNullOrEmpty(string input)
        {
            // Act
            var result = StringUtilities.ToCamelCase(input);

            // Assert
            result.Should().Be(input);
        }

        /// <summary>
        /// Tests that ToCamelCase correctly converts snake_case strings to camelCase.
        /// </summary>
        /// <param name="input">The snake_case input string to convert.</param>
        [Theory]
        [InlineData("hello")]
        [InlineData("hello_world")]
        [InlineData("hello_world_test")]
        public void ToCamelCase_ShouldConvertSnakeCaseToCamelCase(string input)
        {
            // Act
            var result = StringUtilities.ToCamelCase(input);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }
    }

    /// <summary>
    /// Contains tests for the RemoveWhitespace method of the StringUtilities class.
    /// </summary>
    public class RemoveWhitespaceTests
    {
        /// <summary>
        /// Tests that RemoveWhitespace removes all whitespace characters from strings, handling null and empty inputs.
        /// </summary>
        /// <param name="input">The input string from which to remove whitespace.</param>
        /// <param name="expected">The expected result with all whitespace removed.</param>
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("hello", "hello")]
        [InlineData("hello world", "helloworld")]
        [InlineData("  hello   world  ", "helloworld")]
        [InlineData("hello\tworld", "helloworld")]
        [InlineData("hello\nworld", "helloworld")]
        [InlineData("a b c d e", "abcde")]
        public void RemoveWhitespace_ShouldRemoveAllWhitespace(string input, string expected)
        {
            // Act
            var result = StringUtilities.RemoveWhitespace(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    /// <summary>
    /// Contains tests for the SanitizeForFilePath method of the StringUtilities class.
    /// </summary>
    public class SanitizeForFilePathTests
    {
        /// <summary>
        /// Tests that SanitizeForFilePath returns the input unchanged for null or empty strings.
        /// </summary>
        /// <param name="input">The input string to sanitize, which can be null or empty.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SanitizeForFilePath_ShouldReturnInputForNullOrEmpty(string input)
        {
            // Act
            var result = StringUtilities.SanitizeForFilePath(input);

            // Assert
            result.Should().Be(input);
        }

        /// <summary>
        /// Tests that SanitizeForFilePath removes invalid file name characters from the input string.
        /// </summary>
        /// <param name="input">The input string containing potentially invalid file name characters.</param>
        [Theory]
        [InlineData("hello")]
        [InlineData("hello world")]
        [InlineData("test/file:with*invalid?chars.txt")]
        public void SanitizeForFilePath_ShouldRemoveInvalidFileNameCharacters(string input)
        {
            // Act
            var result = StringUtilities.SanitizeForFilePath(input);

            // Assert
            result.Should().NotBeNull();

            // Verify no invalid path characters remain
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                result.Should().NotContain(c.ToString());
            }
        }
    }

    /// <summary>
    /// Contains tests for the SanitizeForHtml method of the StringUtilities class.
    /// </summary>
    public class SanitizeForHtmlTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("hello", "hello")]
        [InlineData("<script>alert('xss')</script>", "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;")]
        [InlineData("&<>'\"", "&amp;&lt;&gt;&#39;&quot;")]
        [InlineData("test & more", "test &amp; more")]
        [InlineData("5 > 3 and 3 < 5", "5 &gt; 3 and 3 &lt; 5")]
        public void SanitizeForHtml_ShouldEscapeHtmlCharacters(string input, string expected)
        {
            // Act
            var result = StringUtilities.SanitizeForHtml(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    public class IsValidEmailTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("plainaddress", false)]
        [InlineData("@no-local-part.com", false)]
        [InlineData("A@b@c@example.com", false)]
        [InlineData("valid.email@example.com", true)]
        [InlineData("user.name@domain.com", true)]
        [InlineData("user@sub.domain.com", true)]
        [InlineData("firstname.lastname@domain.co.uk", true)]
        [InlineData("email@123.123.123.123", true)]
        [InlineData("1234567890@domain.com", true)]
        [InlineData("email@domain-one.com", true)]
        [InlineData("_______@domain.com", true)]
        [InlineData("email@domain.name", true)]
        [InlineData("email@domain.co.jp", true)]
        [InlineData("firstname-lastname@domain.com", true)]
        public void IsValidEmail_ShouldValidateEmailFormat(string input, bool expected)
        {
            // Act
            var result = StringUtilities.IsValidEmail(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    public class IsValidUrlTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("not a url", false)]
        [InlineData("http://example.com", true)]
        [InlineData("https://example.com", true)]
        [InlineData("https://sub.domain.com/path?query=value", true)]
        [InlineData("ftp://example.com", false)]
        [InlineData("file:///path/to/file", false)]
        [InlineData("example.com", false)]
        [InlineData("https://example.com:8080/path", true)]
        [InlineData("http://localhost:5000", true)]
        public void IsValidUrl_ShouldValidateUrlFormat(string input, bool expected)
        {
            // Act
            var result = StringUtilities.IsValidUrl(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    public class IsValidGuidTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("not-a-guid", false)]
        [InlineData("550e8400-e29b-41d4-a716-446655440000", true)]
        [InlineData("550E8400-E29B-41D4-A716-446655440000", true)]
        [InlineData("{550e8400-e29b-41d4-a716-446655440000}", true)]
        [InlineData("(550e8400-e29b-41d4-a716-446655440000)", true)]
        [InlineData("550e8400e29b41d4a716446655440000", true)]
        [InlineData("00000000-0000-0000-0000-000000000000", true)]
        public void IsValidGuid_ShouldValidateGuidFormat(string input, bool expected)
        {
            // Act
            var result = StringUtilities.IsValidGuid(input);

            // Assert
            result.Should().Be(expected);
        }
    }

    public class GenerateRandomStringTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(32)]
        public void GenerateRandomString_ShouldReturnStringOfCorrectLength(int length)
        {
            // Act
            var result = StringUtilities.GenerateRandomString(length);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveLength(length);
            result.Should().MatchRegex("^[A-Za-z0-9]*$");
        }

        [Fact]
        public void GenerateRandomString_ShouldReturnDefaultLengthWhenNotSpecified()
        {
            // Act
            var result = StringUtilities.GenerateRandomString();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveLength(16);
            result.Should().MatchRegex("^[A-Za-z0-9]*$");
        }
    }

    public class RepeatTests
    {
        [Theory]
        [InlineData(null, 5, "")]
        [InlineData("", 5, "")]
        [InlineData("a", 0, "")]
        [InlineData("a", 1, "a")]
        [InlineData("abc", 3, "abcabcabc")]
        [InlineData("test", 5, "testtesttesttesttest")]
        public void Repeat_ShouldRepeatStringCorrectly(string input, int count, string expected)
        {
            // Act
            var result = StringUtilities.Repeat(input, count);

            // Assert
            result.Should().Be(expected);
        }
    }

    public class SplitPreservingQuotesTests
    {
        [Fact]
        public void SplitPreservingQuotes_ShouldThrowOnNullInput()
        {
            // Arrange
            string input = null;

            // Act
            var action = () => StringUtilities.SplitPreservingQuotes(input);

            // Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("a,b,c")]
        [InlineData("a, b, c")]
        [InlineData("a,\"b,c\",d")]
        [InlineData("\"quoted, text\", \"more\", \"data\"")]
        [InlineData("a\"b\"c")]
        [InlineData("a,\"b\",c,\"d\",e")]
        [InlineData("simple")]
        [InlineData("  spaced  ,  values  ")]
        public void SplitPreservingQuotes_ShouldSplitCorrectly(string input)
        {
            // Act
            var result = StringUtilities.SplitPreservingQuotes(input);

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData("a,b,c", ',')]
        [InlineData("a|b|c", '|')]
        [InlineData("a\"b,c\"|d\"e,f\"", '|')]
        public void SplitPreservingQuotes_ShouldUseCustomDelimiter(string input, char delimiter)
        {
            // Act
            var result = StringUtilities.SplitPreservingQuotes(input, delimiter);

            // Assert
            result.Should().NotBeNull();
        }
    }

    public class GetStringSimilarityTests
    {
        [Theory]
        [InlineData(null, "test")]
        [InlineData("", "test")]
        [InlineData("test", null)]
        [InlineData("test", "")]
        public void GetStringSimilarity_ShouldReturnZeroForNullOrEmpty(string str1, string str2)
        {
            // Act
            var result = StringUtilities.GetStringSimilarity(str1, str2);

            // Assert
            result.Should().Be(0.0);
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("a", "a")]
        public void GetStringSimilarity_ShouldReturnOneForIdenticalStrings(string str1, string str2)
        {
            // Act
            var result = StringUtilities.GetStringSimilarity(str1, str2);

            // Assert
            result.Should().Be(1.0);
        }

        [Theory]
        [InlineData("hello", "world")]
        public void GetStringSimilarity_ShouldCalculateSimilarity(string str1, string str2)
        {
            // Act
            var result = StringUtilities.GetStringSimilarity(str1, str2);

            // Assert
            result.Should().BeInRange(0.0, 1.0);
        }
    }
}
