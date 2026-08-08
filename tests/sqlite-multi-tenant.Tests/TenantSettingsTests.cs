using System;
using System.Collections.Generic;
using Xunit;
using SqliteMultiTenant.Models;

namespace SqliteMultiTenant.Tests
{
    public sealed class TenantSettingsTests
    {
        [Fact]
        public void Constructor_InitializesPropertiesAsExpected()
        {
            // Arrange & Act
            var settings = new TenantSettings();

            // Assert
            Assert.Equal(string.Empty, settings.SettingId);
            Assert.Equal(string.Empty, settings.TenantId);
            Assert.Equal(string.Empty, settings.SettingKey);
            Assert.Equal(string.Empty, settings.SettingValue);
            Assert.Null(settings.Description);
            Assert.Null(settings.DataType);
            Assert.False(settings.IsEncrypted);
            Assert.InRange(settings.CreatedAt, DateTime.UtcNow.AddSeconds(-2), DateTime.UtcNow.AddSeconds(2));
            Assert.InRange(settings.UpdatedAt, DateTime.UtcNow.AddSeconds(-2), DateTime.UtcNow.AddSeconds(2));
            Assert.Null(settings.LastModifiedBy);
            Assert.True(settings.IsActive);
            Assert.Null(settings.Tenant);
        }

        [Fact]
        public void Validate_WithValidSettings_ReturnsTrueAndEmptyErrors()
        {
            // Arrange
            var settings = new TenantSettings
            {
                SettingId = "guid",
                TenantId = "tenant",
                SettingKey = "key",
                SettingValue = "value"
            };

            // Act
            var result = settings.Validate(out var errors);

            // Assert
            Assert.True(result);
            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_WithMissingRequiredProperties_ReturnsFalseAndErrors()
        {
            // Arrange
            var settings = new TenantSettings(); // all required properties are empty string

            // Act
            var result = settings.Validate(out var errors);

            // Assert
            Assert.False(result);
            Assert.Contains(errors, e => e == "SettingId is required");
            Assert.Contains(errors, e => e == "TenantId is required");
            Assert.Contains(errors, e => e == "SettingKey is required");
            Assert.Contains(errors, e => e == "SettingValue is required");
        }

        [Fact]
        public void Validate_WithSettingKeyExceedingMaxLength_ReturnsFalseAndError()
        {
            // Arrange
            var settings = new TenantSettings
            {
                SettingId = "guid",
                TenantId = "tenant",
                SettingKey = new string('a', 257), // 257 > 256
                SettingValue = "value"
            };

            // Act
            var result = settings.Validate(out var errors);

            // Assert
            Assert.False(result);
            Assert.Contains(errors, e => e == "SettingKey exceeds maximum length");
        }

        [Fact]
        public void UpdateValue_UpdatesSettingValueAndUpdatedAtAndLastModifiedBy()
        {
            // Arrange
            var settings = new TenantSettings();
            var originalUpdatedAt = settings.UpdatedAt;
            var modifiedBy = "tester";

            // Act
            settings.UpdateValue("new value", modifiedBy);

            // Assert
            Assert.Equal("new value", settings.SettingValue);
            Assert.NotEqual(originalUpdatedAt, settings.UpdatedAt);
            Assert.Equal(modifiedBy, settings.LastModifiedBy);
        }

        [Fact]
        public void SetActive_TogglesIsActiveAndUpdatesUpdatedAt()
        {
            // Arrange
            var settings = new TenantSettings();
            var originalUpdatedAt = settings.UpdatedAt;

            // Act
            settings.SetActive(false);

            // Assert
            Assert.False(settings.IsActive);
            Assert.NotEqual(originalUpdatedAt, settings.UpdatedAt);

            // Act
            settings.SetActive(true);

            // Assert
            Assert.True(settings.IsActive);
            // UpdatedAt should have been updated again
            Assert.NotEqual(settings.UpdatedAt, originalUpdatedAt);
        }

        [Fact]
        public void GetValue_WithValidConvertibleValue_ReturnsCorrectType()
        {
            // Arrange
            var settings = new TenantSettings { SettingValue = "42" };

            // Act
            var result = settings.GetValue<int>();

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void GetValue_WithInvalidConvertibleValue_ThrowsInvalidOperationException()
        {
            // Arrange
            var settings = new TenantSettings { SettingValue = "not a number" };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => settings.GetValue<int>());
        }

        [Fact]
        public void SetValue_WithIntegerValue_SetsSettingValueAndDataTypeAndUpdatedAtAndLastModifiedBy()
        {
            // Arrange
            var settings = new TenantSettings();
            var modifiedBy = "tester";

            // Act
            settings.SetValue(123, modifiedBy);

            // Assert
            Assert.Equal("123", settings.SettingValue);
            Assert.Equal("Int32", settings.DataType); // Note: typeof(int).Name is "Int32"
            Assert.NotEqual(settings.CreatedAt, settings.UpdatedAt); // UpdatedAt should have changed
            Assert.Equal(modifiedBy, settings.LastModifiedBy);
        }

        [Fact]
        public void SetValue_WithNullString_SetsSettingValueToEmptyStringAndDataTypeToString()
        {
            // Arrange
            var settings = new TenantSettings();
            var modifiedBy = "tester";

            // Act
            settings.SetValue<string>(null, modifiedBy);

            // Assert
            Assert.Equal(string.Empty, settings.SettingValue);
            Assert.Equal("String", settings.DataType);
            Assert.NotEqual(settings.CreatedAt, settings.UpdatedAt);
            Assert.Equal(modifiedBy, settings.LastModifiedBy);
        }
    }
}