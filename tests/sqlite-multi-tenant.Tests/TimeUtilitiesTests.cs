using System;
using Xunit;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests
{
    public class TimeUtilitiesTests
    {
        [Fact]
        public void FormatTimeSpan_ZeroSpan_ReturnsLessThanSecond()
        {
            // Arrange
            var span = TimeSpan.Zero;

            // Act
            var result = TimeUtilities.FormatTimeSpan(span);

            // Assert
            Assert.Equal("less than a second", result);
        }

        [Fact]
        public void FormatTimeSpan_SingleDay_ReturnsCorrectString()
        {
            // Arrange
            var span = new TimeSpan(1, 0, 0, 0); // 1 day

            // Act
            var result = TimeUtilities.FormatTimeSpan(span);

            // Assert
            Assert.Equal("1 day", result);
        }

        [Fact]
        public void FormatTimeSpan_MultipleComponents_ReturnsCorrectString()
        {
            // Arrange
            var span = new TimeSpan(2, 3, 4, 5); // 2 days, 3 hours, 4 minutes, 5 seconds

            // Act
            var result = TimeUtilities.FormatTimeSpan(span);

            // Assert
            Assert.Equal("2 days, 3 hours, 4 minutes", result); // seconds omitted because we already have 3 parts
        }

        [Fact]
        public void FormatRelativeTime_JustNow_ReturnsJustNow()
        {
            // Arrange
            var dateTime = DateTime.UtcNow.AddSeconds(-30); // 30 seconds ago

            // Act
            var result = TimeUtilities.FormatRelativeTime(dateTime);

            // Assert
            Assert.Equal("just now", result);
        }

        [Fact]
        public void FormatRelativeTime_OneMinuteAgo_ReturnsCorrectString()
        {
            // Arrange
            var dateTime = DateTime.UtcNow.AddMinutes(-1);

            // Act
            var result = TimeUtilities.FormatRelativeTime(dateTime);

            // Assert
            Assert.Equal("1 minutes ago", result);
        }

        [Fact]
        public void FormatRelativeTime_TwoHoursAgo_ReturnsCorrectString()
        {
            // Arrange
            var dateTime = DateTime.UtcNow.AddHours(-2);

            // Act
            var result = TimeUtilities.FormatRelativeTime(dateTime);

            // Assert
            Assert.Equal("2 hours ago", result);
        }

        [Fact]
        public void RoundToNearest_RoundsToNearestMinute()
        {
            // Arrange
            var dateTime = new DateTime(2020, 1, 1, 12, 32, 15); // 32 minutes, 15 seconds
            var interval = TimeSpan.FromMinutes(1); // 1 minute

            // Act
            var result = TimeUtilities.RoundToNearest(dateTime, interval);

            // Assert
            // 32 minutes 15 seconds rounds to 32 minutes (since 15 < 30)
            Assert.Equal(new DateTime(2020, 1, 1, 12, 32, 0), result);
        }

        [Fact]
        public void GetStartOfDay_ReturnsMidnight()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 10, 14, 30, 45);

            // Act
            var result = TimeUtilities.GetStartOfDay(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 5, 10, 0, 0, 0), result);
        }

        [Fact]
        public void GetEndOfDay_ReturnsLastTickOfDay()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 10, 14, 30, 45);

            // Act
            var result = TimeUtilities.GetEndOfDay(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 5, 10, 23, 59, 59, 999), result);
        }

        [Fact]
        public void GetStartOfWeek_ReturnsMonday()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 12); // Tuesday 2020-05-12

            // Act
            var result = TimeUtilities.GetStartOfWeek(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 5, 11), result); // Monday 2020-05-11
        }

        [Fact]
        public void GetEndOfWeek_ReturnsSundayEnd()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 12); // Tuesday

            // Act
            var result = TimeUtilities.GetEndOfWeek(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 5, 17, 23, 59, 59, 999), result); // Sunday end of day
        }

        [Fact]
        public void GetStartOfMonth_ReturnsFirstDay()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 15);

            // Act
            var result = TimeUtilities.GetStartOfMonth(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 5, 1), result);
        }

        [Fact]
        public void GetEndOfMonth_ReturnsLastDay()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 15);

            // Act
            var result = TimeUtilities.GetEndOfMonth(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 5, 31, 23, 59, 59, 999), result);
        }

        [Fact]
        public void GetStartOfYear_ReturnsJanuaryFirst()
        {
            // Arrange
            var dateTime = new DateTime(2020, 5, 15);

            // Act
            var result = TimeUtilities.GetStartOfYear(dateTime);

            // Assert
            Assert.Equal(new DateTime(2020, 1, 1), result);
        }
    }
}