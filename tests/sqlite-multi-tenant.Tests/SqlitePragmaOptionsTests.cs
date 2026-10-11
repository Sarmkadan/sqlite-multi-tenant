#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Database;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Unit tests for <see cref="SqlitePragmaOptions"/> validation and its startup wiring
/// through <see cref="ConnectionPoolOptions"/>, <see cref="ConnectionPoolManager"/>,
/// <see cref="ConnectionPool"/> and <see cref="ConnectionPoolingExtensions"/>.
/// </summary>
public sealed class SqlitePragmaOptionsTests
{
    [Fact]
    public void Validate_DefaultValues_DoesNotThrow()
    {
        // Arrange
        var options = new SqlitePragmaOptions();

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
        options.JournalMode.Should().Be("WAL");
        options.Synchronous.Should().Be("NORMAL");
        options.BusyTimeoutMs.Should().Be(5000);
        options.ForeignKeys.Should().BeTrue();
    }

    [Theory]
    [InlineData("wal")]
    [InlineData("DELETE")]
    [InlineData("Truncate")]
    [InlineData("persist")]
    [InlineData("memory")]
    [InlineData("off")]
    public void Validate_AllowedJournalModeInAnyCase_DoesNotThrow(string journalMode)
    {
        // Arrange
        var options = new SqlitePragmaOptions { JournalMode = journalMode };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_UnknownJournalMode_ThrowsOptionsValidationException()
    {
        // Arrange
        var options = new SqlitePragmaOptions { JournalMode = "banana" };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*JournalMode 'banana' is not allowed*WAL*");
    }

    [Fact]
    public void Validate_UnknownSynchronous_ThrowsOptionsValidationException()
    {
        // Arrange
        var options = new SqlitePragmaOptions { Synchronous = "sometimes" };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Synchronous 'sometimes' is not allowed*");
    }

    [Fact]
    public void Validate_NegativeBusyTimeout_ThrowsOptionsValidationException()
    {
        // Arrange
        var options = new SqlitePragmaOptions { BusyTimeoutMs = -1 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*BusyTimeoutMs must be zero or greater*");
    }

    [Fact]
    public void Validate_MultipleInvalidValues_ReportsEveryFailure()
    {
        // Arrange
        var options = new SqlitePragmaOptions { JournalMode = "banana", Synchronous = "maybe", BusyTimeoutMs = -5 };

        // Act
        var act = () => options.Validate();

        // Assert
        var exception = act.Should().Throw<OptionsValidationException>().Which;
        exception.Failures.Should().HaveCount(3);
    }

    [Fact]
    public void ConnectionPoolOptionsValidate_InvalidJournalMode_ThrowsOptionsValidationException()
    {
        // Arrange
        var options = new ConnectionPoolOptions { Pragmas = new SqlitePragmaOptions { JournalMode = "banana" } };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ConnectionPoolManager_Constructor_InvalidJournalMode_ThrowsOptionsValidationException()
    {
        // Arrange
        var options = new ConnectionPoolOptions { Pragmas = new SqlitePragmaOptions { JournalMode = "banana" } };

        // Act
        var act = () => new ConnectionPoolManager(options, NullLogger<ConnectionPoolManager>.Instance);

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AddConnectionPooling_InvalidJournalMode_ThrowsOptionsValidationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddConnectionPooling(o => o.Pragmas.JournalMode = "banana");

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ConnectionPool_Constructor_InvalidSynchronous_ThrowsOptionsValidationException()
    {
        // Arrange
        var pragmas = new SqlitePragmaOptions { Synchronous = "sometimes" };

        // Act
        var act = () => new ConnectionPool("Data Source=:memory:pragmaunit", NullLogger<ConnectionPool>.Instance, pragmas: pragmas);

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }
}
