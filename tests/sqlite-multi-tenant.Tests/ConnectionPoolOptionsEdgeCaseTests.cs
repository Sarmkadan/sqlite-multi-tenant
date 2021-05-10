#nullable enable
using FluentAssertions;
using SqliteMultiTenant.Database;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Edge-case and boundary-value tests for ConnectionPoolOptions validation.
/// Verifies that all invalid configurations are properly rejected.
/// </summary>
public sealed class ConnectionPoolOptionsEdgeCaseTests
{
    [Fact]
    public void Validate_DefaultValues_DoesNotThrow()
    {
        var options = new ConnectionPoolOptions();

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NegativeMinPoolSize_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { MinPoolSize = -1 };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("MinPoolSize");
    }

    [Fact]
    public void Validate_ZeroMinPoolSize_DoesNotThrow()
    {
        var options = new ConnectionPoolOptions { MinPoolSize = 0 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroMaxPoolSize_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { MaxPoolSize = 0 };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("MaxPoolSize");
    }

    [Fact]
    public void Validate_MinPoolSizeGreaterThanMaxPoolSize_ThrowsArgumentException()
    {
        var options = new ConnectionPoolOptions { MinPoolSize = 20, MaxPoolSize = 10 };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_MinPoolSizeEqualsMaxPoolSize_DoesNotThrow()
    {
        var options = new ConnectionPoolOptions { MinPoolSize = 5, MaxPoolSize = 5 };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroIdleTimeout_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { IdleTimeout = TimeSpan.Zero };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("IdleTimeout");
    }

    [Fact]
    public void Validate_NegativeIdleTimeout_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { IdleTimeout = TimeSpan.FromSeconds(-1) };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("IdleTimeout");
    }

    [Fact]
    public void Validate_ZeroAcquireTimeout_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { AcquireTimeout = TimeSpan.Zero };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("AcquireTimeout");
    }

    [Fact]
    public void Validate_ZeroMaxConnectionLifetime_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { MaxConnectionLifetime = TimeSpan.Zero };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("MaxConnectionLifetime");
    }

    [Fact]
    public void Validate_ZeroPruneInterval_ThrowsArgumentOutOfRange()
    {
        var options = new ConnectionPoolOptions { PruneInterval = TimeSpan.Zero };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("PruneInterval");
    }

    [Fact]
    public void Validate_VerySmallPositiveTimeSpans_DoesNotThrow()
    {
        var options = new ConnectionPoolOptions
        {
            IdleTimeout = TimeSpan.FromTicks(1),
            AcquireTimeout = TimeSpan.FromTicks(1),
            MaxConnectionLifetime = TimeSpan.FromTicks(1),
            PruneInterval = TimeSpan.FromTicks(1)
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void PoolStatisticsSnapshot_DefaultValues_AreZeroOrEmpty()
    {
        var snapshot = new PoolStatisticsSnapshot();

        snapshot.TenantId.Should().BeEmpty();
        snapshot.Available.Should().Be(0);
        snapshot.Total.Should().Be(0);
        snapshot.Waiting.Should().Be(0);
        snapshot.PrunedTotal.Should().Be(0);
    }
}
