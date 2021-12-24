#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using SqliteMultiTenant.Database;
using System;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Tests for the ConnectionPoolOptions class.
    /// </summary>
    public sealed class ConnectionPoolOptionsTests {
        /// <summary>
        /// Verifies that Validate does not throw an exception when given valid options.
        /// </summary>
        [Fact]
        public void Validate_WithValidOptions_DoesNotThrow()
        {
            // Arrange
            var options = new ConnectionPoolOptions
            {
                MinPoolSize = 1,
                MaxPoolSize = 10,
                IdleTimeout = TimeSpan.FromMinutes(5),
                AcquireTimeout = TimeSpan.FromSeconds(30),
                MaxConnectionLifetime = TimeSpan.FromHours(1),
                PruneInterval = TimeSpan.FromSeconds(60)
            };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that Validate does not throw an exception when MinPoolSize is zero.
        /// </summary>
        [Fact]
        public void Validate_WithMinPoolSizeZero_DoesNotThrow()
        {
            // Arrange
            var options = new ConnectionPoolOptions
            {
                MinPoolSize = 0, // Valid
                MaxPoolSize = 1,
                IdleTimeout = TimeSpan.FromMinutes(1),
                AcquireTimeout = TimeSpan.FromSeconds(1),
                MaxConnectionLifetime = TimeSpan.FromHours(1),
                PruneInterval = TimeSpan.FromSeconds(1)
            };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that Validate throws an exception when MinPoolSize is negative.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenMinPoolSizeIsNegative()
        {
            // Arrange
            var options = new ConnectionPoolOptions { MinPoolSize = -1 };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("MinPoolSize")
                .WithMessage("MinPoolSize must be non-negative. (Parameter 'MinPoolSize')");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when MaxPoolSize is zero.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenMaxPoolSizeIsZero()
        {
            // Arrange
            var options = new ConnectionPoolOptions { MinPoolSize = 0, MaxPoolSize = 0 };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("MaxPoolSize")
                .WithMessage("MaxPoolSize must be at least 1. (Parameter 'MaxPoolSize')");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when MaxPoolSize is negative.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenMaxPoolSizeIsNegative()
        {
            // Arrange
            var options = new ConnectionPoolOptions { MinPoolSize = 0, MaxPoolSize = -1 };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("MaxPoolSize")
                .WithMessage("MaxPoolSize must be at least 1. (Parameter 'MaxPoolSize')");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when MinPoolSize exceeds MaxPoolSize.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentException_WhenMinPoolSizeExceedsMaxPoolSize()
        {
            // Arrange
            var options = new ConnectionPoolOptions { MinPoolSize = 10, MaxPoolSize = 5 };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("MinPoolSize cannot exceed MaxPoolSize.");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when IdleTimeout is zero.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenIdleTimeoutIsZero()
        {
            // Arrange
            var options = new ConnectionPoolOptions { IdleTimeout = TimeSpan.Zero };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("IdleTimeout")
                .WithMessage("IdleTimeout must be positive. (Parameter 'IdleTimeout')");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when AcquireTimeout is zero.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenAcquireTimeoutIsZero()
        {
            // Arrange
            var options = new ConnectionPoolOptions { AcquireTimeout = TimeSpan.Zero };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("AcquireTimeout")
                .WithMessage("AcquireTimeout must be positive. (Parameter 'AcquireTimeout')");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when MaxConnectionLifetime is zero.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenMaxConnectionLifetimeIsZero()
        {
            // Arrange
            var options = new ConnectionPoolOptions { MaxConnectionLifetime = TimeSpan.Zero };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("MaxConnectionLifetime")
                .WithMessage("MaxConnectionLifetime must be positive. (Parameter 'MaxConnectionLifetime')");
        }

        /// <summary>
        /// Verifies that Validate throws an exception when PruneInterval is zero.
        /// </summary>
        [Fact]
        public void Validate_ThrowsArgumentOutOfRangeException_WhenPruneIntervalIsZero()
        {
            // Arrange
            var options = new ConnectionPoolOptions { PruneInterval = TimeSpan.Zero };

            // Act
            Action act = () => options.Validate();

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("PruneInterval")
                .WithMessage("PruneInterval must be positive. (Parameter 'PruneInterval')");
        }
    }
}
