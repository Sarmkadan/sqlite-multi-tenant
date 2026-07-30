using System;
using Xunit;
using SqliteMultiTenant.Configuration;

namespace SqliteMultiTenant.Tests
{
    public class OptionsValidatorTests
    {
        [Fact]
        public void Validate_MultiTenantOptions_HappyPath()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "/var/data",
                MaxConnectionsPerTenant = 5,
                MaxBackupCount = 7,
                BackupRetention = TimeSpan.FromDays(10)
            };

            // Act & Assert
            OptionsValidator.Validate(options);
        }

        [Fact]
        public void Validate_MultiTenantOptions_NullBasePath_ThrowsArgumentException()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = null,
                MaxConnectionsPerTenant = 5,
                MaxBackupCount = 7,
                BackupRetention = TimeSpan.FromDays(10)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
        }

        [Fact]
        public void Validate_MultiTenantOptions_InvalidMaxConnectionsPerTenant_ThrowsArgumentException()
        {
            // Arrange
            var options = new MultiTenantOptions
            {
                BasePath = "/var/data",
                MaxConnectionsPerTenant = 0,
                MaxBackupCount = 7,
                BackupRetention = TimeSpan.FromDays(10)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
        }

        [Fact]
        public void Validate_BackupOptions_HappyPath()
        {
            // Arrange
            var options = new BackupOptions
            {
                MaxConcurrentBackups = 5,
                BackupTimeoutSeconds = 30
            };

            // Act & Assert
            OptionsValidator.Validate(options);
        }

        [Fact]
        public void Validate_SecurityOptions_HappyPath()
        {
            // Arrange
            var options = new SecurityOptions
            {
                SessionTimeout = TimeSpan.FromHours(1),
                MaxFailedLoginAttempts = 5,
                LockoutDuration = TimeSpan.FromMinutes(30)
            };

            // Act & Assert
            OptionsValidator.Validate(options);
        }

        [Fact]
        public void Validate_SecurityOptions_InvalidSessionTimeout_ThrowsArgumentException()
        {
            // Arrange
            var options = new SecurityOptions
            {
                SessionTimeout = TimeSpan.Zero,
                MaxFailedLoginAttempts = 5,
                LockoutDuration = TimeSpan.FromMinutes(30)
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
        }
    }
}
