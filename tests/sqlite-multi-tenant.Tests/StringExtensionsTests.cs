using System;
using System.Collections.Generic;
using SqliteMultiTenant.Utilities;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class StringExtensionsTests
    {
        #region ToSafeDatabaseIdentifier

        [Fact]
        public void ToSafeDatabaseIdentifier_ConvertsToSnakeCaseAndHandlesDigits()
        {
            // Normal conversion
            var result = "My Table-Name 123".ToSafeDatabaseIdentifier();
            Assert.Equal("my_table_name_123", result);

            // Leading digit should be prefixed with underscore
            var leadingDigit = "9example".ToSafeDatabaseIdentifier();
            Assert.Equal("_9example", leadingDigit);
        }

        [Fact]
        public void ToSafeDatabaseIdentifier_ReturnsEmptyForNullOrWhiteSpace()
        {
            Assert.Empty("   ".ToSafeDatabaseIdentifier());

            // Null should throw ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => ((string)null!).ToSafeDatabaseIdentifier());
        }

        #endregion

        #region SafeTruncate

        [Fact]
        public void SafeTruncate_TruncatesAndAddsEllipsis()
        {
            var input = "HelloWorld";
            var truncated = input.SafeTruncate(5);
            Assert.Equal("He...", truncated);
        }

        [Fact]
        public void SafeTruncate_WithoutEllipsis_ReturnsExactLength()
        {
            var input = "HelloWorld";
            var truncated = input.SafeTruncate(5, addEllipsis: false);
            Assert.Equal("Hello", truncated);
        }

        [Fact]
        public void SafeTruncate_ReturnsOriginalWhenShorter()
        {
            var input = "short";
            var result = input.SafeTruncate(10);
            Assert.Equal(input, result);
        }

        [Fact]
        public void SafeTruncate_NegativeLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => "test".SafeTruncate(-1));
        }

        [Fact]
        public void SafeTruncate_NullInput_ReturnsNull()
        {
            string? nullStr = null;
            var result = nullStr.SafeTruncate(5);
            Assert.Null(result);
        }

        #endregion

        #region IsValidTenantIdentifier

        [Fact]
        public void IsValidTenantIdentifier_AcceptsGuid()
        {
            var guid = Guid.NewGuid().ToString();
            Assert.True(guid.IsValidTenantIdentifier());
        }

        [Fact]
        public void IsValidTenantIdentifier_AcceptsSlug()
        {
            Assert.True("tenant-123".IsValidTenantIdentifier());
        }

        [Fact]
        public void IsValidTenantIdentifier_RejectsInvalidValues()
        {
            Assert.False("".IsValidTenantIdentifier());
            Assert.False("   ".IsValidTenantIdentifier());
            // Too long slug (101 chars)
            var longSlug = new string('a', 101);
            Assert.False(longSlug.IsValidTenantIdentifier());
            // Invalid characters
            Assert.False("invalid$slug".IsValidTenantIdentifier());
        }

        #endregion

        #region ToEnum

        private enum SampleEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public void ToEnum_ParsesValidValue_IgnoringCase()
        {
            var result = "second".ToEnum(SampleEnum.First);
            Assert.Equal(SampleEnum.Second, result);
        }

        [Fact]
        public void ToEnum_InvalidValue_ReturnsDefault()
        {
            var result = "unknown".ToEnum(SampleEnum.Third);
            Assert.Equal(SampleEnum.Third, result);
        }

        [Fact]
        public void ToEnum_NullOrWhiteSpace_ReturnsDefault()
        {
            string? nullStr = null;
            var result1 = nullStr.ToEnum(SampleEnum.First);
            var result2 = "".ToEnum(SampleEnum.First);
            Assert.Equal(SampleEnum.First, result1);
            Assert.Equal(SampleEnum.First, result2);
        }

        #endregion

        #region EscapeForJson

        [Fact]
        public void EscapeForJson_EscapesSpecialCharacters()
        {
            var input = "\"Hello\\World\b\f\n\r\t\"";
            var escaped = input.EscapeForJson();
            Assert.Equal("\\\"Hello\\\\World\\b\\f\\n\\r\\t\\\"", escaped);
        }

        [Fact]
        public void EscapeForJson_NullOrEmpty_ReturnsSame()
        {
            string? nullStr = null;
            Assert.Null(nullStr.EscapeForJson());
            Assert.Equal(string.Empty, string.Empty.EscapeForJson());
        }

        #endregion

        #region ContainsForbiddenCharacters

        [Fact]
        public void ContainsForbiddenCharacters_DetectsPresence_IgnoringCase()
        {
            var forbidden = new[] { "DROP", "DELETE" };
            Assert.True("This string contains drop table".ContainsForbiddenCharacters(forbidden));
            Assert.False("Safe string".ContainsForbiddenCharacters(forbidden));
        }

        [Fact]
        public void ContainsForbiddenCharacters_NullArray_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => "test".ContainsForbiddenCharacters(null!));
        }

        #endregion

        #region NormalizeWhitespace

        [Fact]
        public void NormalizeWhitespace_CollapsesSpacesAndTrims()
        {
            var input = "  This   is   a   test \t ";
            var normalized = input.NormalizeWhitespace();
            Assert.Equal("This is a test", normalized);
        }

        [Fact]
        public void NormalizeWhitespace_NullOrWhiteSpace_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, ((string?)null).NormalizeWhitespace());
            Assert.Equal(string.Empty, "   ".NormalizeWhitespace());
        }

        #endregion

        #region IsValidFilePath

        [Fact]
        public void IsValidFilePath_AcceptsValidPaths()
        {
            Assert.True("folder/sub/file.txt".IsValidFilePath());
            Assert.True("C:/temp/file.db".IsValidFilePath());
        }

        [Fact]
        public void IsValidFilePath_RejectsTraversalOrInvalidChars()
        {
            Assert.False("../outside.txt".IsValidFilePath());
            Assert.False("folder//file.txt".IsValidFilePath());
            Assert.False("folder\\file?.txt".IsValidFilePath());
        }

        [Fact]
        public void IsValidFilePath_NullOrEmpty_ReturnsFalse()
        {
            Assert.False(((string?)null).IsValidFilePath());
            Assert.False(string.Empty.IsValidFilePath());
        }

        #endregion

        #region Reverse

        [Fact]
        public void Reverse_ReversesString()
        {
            Assert.Equal("cba", "abc".Reverse());
        }

        [Fact]
        public void Reverse_NullOrEmpty_ReturnsSame()
        {
            string? nullStr = null;
            Assert.Null(nullStr.Reverse());
            Assert.Equal(string.Empty, string.Empty.Reverse());
        }

        #endregion
    }
}
