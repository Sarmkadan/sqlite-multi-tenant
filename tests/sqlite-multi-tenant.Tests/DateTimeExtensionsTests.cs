using System;
using Xunit;
using SqliteMultiTenant.Utilities;

namespace SqliteMultiTenant.Tests
{
    public class DateTimeExtensionsTests
    {
        // -----------------------------------------------------------------
        // IsExpired
        // -----------------------------------------------------------------
        [Fact]
        public void IsExpired_FutureDate_ReturnsFalse()
        {
            // Arrange
            var future = DateTime.UtcNow.AddDays(1);

            // Act
            var result = future.IsExpired();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsExpired_PastDate_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var past = DateTime.UtcNow.AddDays(-1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => past.IsExpired());
        }

        // -----------------------------------------------------------------
        // GetAgeDays
        // -----------------------------------------------------------------
        [Fact]
        public void GetAgeDays_PastDate_ReturnsCorrectWholeDays()
        {
            // Arrange
            var created = DateTime.UtcNow.AddDays(-3).AddHours(-5); // 3 days + 5h ago

            // Act
            var days = created.GetAgeDays();

            // Assert
            Assert.Equal(3, days); // truncated
        }

        [Fact]
        public void GetAgeDays_FutureDate_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var future = DateTime.UtcNow.AddHours(2);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => future.GetAgeDays());
        }

        // -----------------------------------------------------------------
        // ToIso8601String
        // -----------------------------------------------------------------
        [Fact]
        public void ToIso8601String_ReturnsCorrectFormat()
        {
            // Arrange
            var dt = new DateTime(2023, 5, 1, 12, 30, 45, DateTimeKind.Local);

            // Act
            var iso = dt.ToIso8601String();

            // Assert
            // The "O" format is invariant, so we can parse it back.
            var parsed = DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind);
            Assert.Equal(dt.ToUniversalTime(), parsed);
        }

        // -----------------------------------------------------------------
        // IsWithinRetentionWindow
        // -----------------------------------------------------------------
        [Fact]
        public void IsWithinRetentionWindow_DateInsideWindow_ReturnsTrue()
        {
            // Arrange
            var retentionDays = 10;
            var date = DateTime.UtcNow.AddDays(-5);

            // Act
            var result = date.IsWithinRetentionWindow(retentionDays);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsWithinRetentionWindow_DateOutsideWindow_ReturnsFalse()
        {
            // Arrange
            var retentionDays = 10;
            var date = DateTime.UtcNow.AddDays(-15);

            // Act
            var result = date.IsWithinRetentionWindow(retentionDays);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsWithinRetentionWindow_NegativeRetention_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var date = DateTime.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => date.IsWithinRetentionWindow(-1));
        }

        // -----------------------------------------------------------------
        // GetNextScheduledTime
        // -----------------------------------------------------------------
        [Fact]
        public void GetNextScheduledTime_FutureBaseTime_ReturnsSameTime()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddMinutes(30);
            var interval = 10;

            // Act
            var next = baseTime.GetNextScheduledTime(interval);

            // Assert
            Assert.Equal(baseTime, next);
        }

        [Fact]
        public void GetNextScheduledTime_PastBaseTime_ReturnsFutureTime()
        {
            // Arrange
            var baseTime = DateTime.UtcNow.AddMinutes(-25);
            var interval = 10; // will add 3 intervals: -25 -> -15 -> -5 -> +5 (>= now)

            // Act
            var next = baseTime.GetNextScheduledTime(interval);

            // Assert
            Assert.True(next >= DateTime.UtcNow);
            // The difference should be less than interval minutes
            var diff = next - DateTime.UtcNow;
            Assert.InRange(diff.TotalMinutes, 0, interval);
        }

        [Fact]
        public void GetNextScheduledTime_NonPositiveInterval_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var baseTime = DateTime.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => baseTime.GetNextScheduledTime(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => baseTime.GetNextScheduledTime(-5));
        }

        // -----------------------------------------------------------------
        // ToHumanReadableDuration
        // -----------------------------------------------------------------
        [Theory]
        [InlineData(0.5, "< 1s")]
        [InlineData(30, "30s")]
        [InlineData(90, "1m 30s")]
        [InlineData(3660, "1h 1m")]
        public void ToHumanReadableDuration_FormatsCorrectly(double totalSeconds, string expected)
        {
            // Arrange
            var span = TimeSpan.FromSeconds(totalSeconds);

            // Act
            var result = span.ToHumanReadableDuration();

            // Assert
            Assert.Equal(expected, result);
        }

        // -----------------------------------------------------------------
        // IsCreatedToday
        // -----------------------------------------------------------------
        [Fact]
        public void IsCreatedToday_TodayDate_ReturnsTrue()
        {
            // Arrange
            var today = DateTime.UtcNow;

            // Act
            var result = today.IsCreatedToday();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCreatedToday_YesterdayDate_ReturnsFalse()
        {
            // Arrange
            var yesterday = DateTime.UtcNow.AddDays(-1);

            // Act
            var result = yesterday.IsCreatedToday();

            // Assert
            Assert.False(result);
        }

        // -----------------------------------------------------------------
        // StartOfDayUtc & EndOfDayUtc
        // -----------------------------------------------------------------
        [Fact]
        public void StartOfDayUtc_ReturnsMidnightUtc()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(13).AddMinutes(45);

            // Act
            var start = now.StartOfDayUtc();

            // Assert
            Assert.Equal(now.Date, start);
            Assert.Equal(0, start.Hour);
            Assert.Equal(0, start.Minute);
            Assert.Equal(0, start.Second);
            Assert.Equal(DateTimeKind.Utc, start.Kind);
        }

        [Fact]
        public void EndOfDayUtc_ReturnsLastTickOfDayUtc()
        {
            // Arrange
            var now = DateTime.UtcNow.AddHours(13).AddMinutes(45);

            // Act
            var end = now.EndOfDayUtc();

            // Assert
            var expected = now.Date.AddDays(1).AddTicks(-1);
            Assert.Equal(expected, end);
            Assert.Equal(DateTimeKind.Utc, end.Kind);
        }

        // -----------------------------------------------------------------
        // RoundDownToMinute
        // -----------------------------------------------------------------
        [Fact]
        public void RoundDownToMinute_TruncatesSecondsAndTicks()
        {
            // Arrange
            var original = new DateTime(2023, 1, 1, 12, 34, 56, 789, DateTimeKind.Utc);

            // Act
            var rounded = original.RoundDownToMinute();

            // Assert
            var expected = new DateTime(2023, 1, 1, 12, 34, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expected, rounded);
        }
    }
}
