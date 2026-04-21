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
    public class MultiTenantOptionsValidationTests
    {
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
