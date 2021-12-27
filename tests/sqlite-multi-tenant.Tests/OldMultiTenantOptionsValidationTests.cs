#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using FluentAssertions;
using SqliteMultiTenant.Configuration;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Contains unit tests that verify the validation logic of <see cref="MultiTenantOptions"/>
    /// and related option classes (<see cref="BackupOptions"/>, <see cref="SecurityOptions"/>).
    /// Each test exercises a specific validation rule and asserts that the expected
    /// exception is thrown (or not thrown) by <see cref="OptionsValidator.Validate"/>.
    /// </summary>
    public sealed class MultiTenantOptionsValidationTests {
        /// <summary>
        /// Verifies that <see cref="OptionsValidator.Validate(MultiTenantOptions)"/> does not
        /// throw an exception when all required properties contain valid values.
        /// </summary>
        [Fact]
        public void Validate_MultiTenantOptions_ShouldNotThrowException_WithValidOptions()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "./databases",
                MaxConnectionsPerTenant = 5,
                MaxBackupCount = 10,
                BackupRetention = TimeSpan.FromDays(30)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="MultiTenantOptions.BasePath"/> is an empty string.
        /// </summary>
        [Fact]
        public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenBasePathIsEmpty()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "",
                MaxConnectionsPerTenant = 5,
                MaxBackupCount = 10,
                BackupRetention = TimeSpan.FromDays(30)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("BasePath cannot be empty");
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="MultiTenantOptions.MaxConnectionsPerTenant"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenMaxConnectionsPerTenantIsZeroOrLess()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "./databases",
                MaxConnectionsPerTenant = 0,
                MaxBackupCount = 10,
                BackupRetention = TimeSpan.FromDays(30)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("MaxConnectionsPerTenant must be greater than 0");
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="MultiTenantOptions.MaxBackupCount"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenMaxBackupCountIsZeroOrLess()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "./databases",
                MaxConnectionsPerTenant = 5,
                MaxBackupCount = 0,
                BackupRetention = TimeSpan.FromDays(30)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("MaxBackupCount must be greater than 0");
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="MultiTenantOptions.BackupRetention"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_MultiTenantOptions_ShouldThrowArgumentException_WhenBackupRetentionIsZeroOrLess()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "./databases",
                MaxConnectionsPerTenant = 5,
                MaxBackupCount = 10,
                BackupRetention = TimeSpan.Zero
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("BackupRetention must be positive");
        }

        /// <summary>
        /// Verifies that <see cref="OptionsValidator.Validate(BackupOptions)"/> does not
        /// throw an exception when the backup options are valid.
        /// </summary>
        [Fact]
        public void Validate_BackupOptions_ShouldNotThrowException_WithValidOptions()
        {
            // Arrange
            var options = new BackupOptions
            {
                MaxConcurrentBackups = 2,
                BackupTimeoutSeconds = 300
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="BackupOptions.MaxConcurrentBackups"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_BackupOptions_ShouldThrowArgumentException_WhenMaxConcurrentBackupsIsZeroOrLess()
        {
            // Arrange
            var options = new BackupOptions
            {
                MaxConcurrentBackups = 0,
                BackupTimeoutSeconds = 300
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("MaxConcurrentBackups must be greater than 0");
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="BackupOptions.BackupTimeoutSeconds"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_BackupOptions_ShouldThrowArgumentException_WhenBackupTimeoutSecondsIsZeroOrLess()
        {
            // Arrange
            var options = new BackupOptions
            {
                MaxConcurrentBackups = 2,
                BackupTimeoutSeconds = 0
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("BackupTimeoutSeconds must be greater than 0");
        }

        /// <summary>
        /// Verifies that <see cref="OptionsValidator.Validate(SecurityOptions)"/> does not
        /// throw an exception when all security options are valid.
        /// </summary>
        [Fact]
        public void Validate_SecurityOptions_ShouldNotThrowException_WithValidOptions()
        {
            // Arrange
            var options = new SecurityOptions
            {
                SessionTimeout = TimeSpan.FromHours(1),
                MaxFailedLoginAttempts = 3,
                LockoutDuration = TimeSpan.FromMinutes(15)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="SecurityOptions.SessionTimeout"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_SecurityOptions_ShouldThrowArgumentException_WhenSessionTimeoutIsZeroOrLess()
        {
            // Arrange
            var options = new SecurityOptions
            {
                SessionTimeout = TimeSpan.Zero,
                MaxFailedLoginAttempts = 3,
                LockoutDuration = TimeSpan.FromMinutes(15)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("SessionTimeout must be positive");
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="SecurityOptions.MaxFailedLoginAttempts"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_SecurityOptions_ShouldThrowArgumentException_WhenMaxFailedLoginAttemptsIsZeroOrLess()
        {
            // Arrange
            var options = new SecurityOptions
            {
                SessionTimeout = TimeSpan.FromHours(1),
                MaxFailedLoginAttempts = 0,
                LockoutDuration = TimeSpan.FromMinutes(15)
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("MaxFailedLoginAttempts must be greater than 0");
        }

        /// <summary>
        /// Verifies that validation throws an <see cref="ArgumentException"/> when
        /// <see cref="SecurityOptions.LockoutDuration"/> is zero or less.
        /// </summary>
        [Fact]
        public void Validate_SecurityOptions_ShouldThrowArgumentException_WhenLockoutDurationIsZeroOrLess()
        {
            // Arrange
            var options = new SecurityOptions
            {
                SessionTimeout = TimeSpan.FromHours(1),
                MaxFailedLoginAttempts = 3,
                LockoutDuration = TimeSpan.Zero
            };

            // Act
            Action act = () => OptionsValidator.Validate(options);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("LockoutDuration must be positive");
        }
    }
}
