#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using NSubstitute;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Models;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class GenericRepositoryTests {
    private class TestEntity { public string Id { get; set; } = string.Empty; }
    
    private class TestGenericRepository : GenericRepository<TestEntity> 
    {
    }

    private readonly TestGenericRepository _repository;

    public GenericRepositoryTests()
    {
        _repository = new TestGenericRepository();
    }

    [Fact]
    public void Items_Initially_ShouldNotBeNull()
    {
        // Assert
        _repository.Items.Should().NotBeNull();
        _repository.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnZeroWhenEmpty()
    {
        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task TransactionMethods_ShouldNotThrow()
    {
        // Act
        var actBegin = async () => await _repository.BeginTransactionAsync();
        var actCommit = async () => await _repository.CommitAsync();
        var actRollback = async () => await _repository.RollbackAsync();

        // Assert
        await actBegin.Should().NotThrowAsync();
        await actCommit.Should().NotThrowAsync();
        await actRollback.Should().NotThrowAsync();
    }
    
    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act
        var act = () => _repository.Dispose();
        
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Items_WhenAdded_ContainsItem()
    {
        // Arrange
        var item = new TestEntity { Id = "1" };
        
        // Act
        _repository.Items.Add(item);
        
        // Assert
        _repository.Items.Should().Contain(item);
        _repository.Items.Count.Should().Be(1);
    }
}