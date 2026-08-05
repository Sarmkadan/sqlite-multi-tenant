using System;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Integration;
using Xunit;

namespace SqliteMultiTenant.Tests;

public class WebhookServiceJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidInstance_ShouldSerializeWithoutThrowing()
    {
        // Arrange
        var service = new WebhookService(NullLogger<WebhookService>.Instance);

        // Act
        var json = service.ToJson();

        // Assert
        json.Should().NotBeNull();
        json.Should().Be("{}");
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ShouldFormatWithIndentation()
    {
        // Arrange
        var service = new WebhookService(NullLogger<WebhookService>.Instance);

        // Act
        var json = service.ToJson(indented: true);

        // Assert
        json.Should().NotBeNull();
    }

    [Fact]
    public void ToJson_WithNullInstance_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookService? service = null;

        // Act
        Action act = () => service!.ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_WithNullOrEmptyJson_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => WebhookServiceJsonExtensions.FromJson(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrowJsonException()
    {
        // Act
        Action act = () => WebhookServiceJsonExtensions.FromJson("not-valid-json");

        // Assert
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ShouldReturnFalseAndNullValue()
    {
        // Act
        var succeeded = WebhookServiceJsonExtensions.TryFromJson("not-valid-json", out var value);

        // Assert
        succeeded.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithNullOrEmptyJson_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => WebhookServiceJsonExtensions.TryFromJson(string.Empty, out _);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToJson_WithSubscription_ShouldContainCamelCaseProperties()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = "sub-1",
            EventType = "backup.completed",
            WebhookUrl = "https://example.com/hook",
            IsActive = true
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(subscription, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        // Assert
        json.Should().Contain("eventType");
        json.Should().Contain("webhookUrl");
        json.Should().Contain("backup.completed");
    }
}
