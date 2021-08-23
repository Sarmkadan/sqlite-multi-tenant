#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Repositories;
using SqliteMultiTenant.Models;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class GenericRepositoryTests {
    private class TestEntity { public string Id { get; set; } = string.Empty; }

    private class TestGenericRepository : GenericRepository<TestEntity>
    {
        public List<TestEntity> Items { get; } = new();

        public TestGenericRepository() : base(NullLogger.Instance)
        {
        }

        public override Task<List<TestEntity>> GetAllAsync() => Task.FromResult(new List<TestEntity>(Items));

        public override Task<TestEntity?> GetByIdAsync(string id) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public override Task<TestEntity> CreateAsync(TestEntity entity)
        {
            Items.Add(entity);
            return Task.FromResult(entity);
        }

        public override Task<bool> UpdateAsync(TestEntity entity)
        {
            var index = Items.FindIndex(i => i.Id == entity.Id);
            if (index < 0)
                return Task.FromResult(false);

            Items[index] = entity;
            return Task.FromResult(true);
        }

        public override Task<bool> DeleteAsync(string id)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item is null)
                return Task.FromResult(false);

            Items.Remove(item);
            return Task.FromResult(true);
        }

        public override Task<List<TestEntity>> FindAsync(Func<TestEntity, bool> predicate) =>
            Task.FromResult(Items.Where(predicate).ToList());

        public override Task<int> GetCountAsync() => Task.FromResult(Items.Count);

        public override Task<bool> ExistsAsync(string id) => Task.FromResult(Items.Any(i => i.Id == id));

        public override Task<int> DeleteAsync(Func<TestEntity, bool> predicate)
        {
            var toRemove = Items.Where(predicate).ToList();
            foreach (var item in toRemove)
                Items.Remove(item);

            return Task.FromResult(toRemove.Count);
        }

        public Task<int> SaveChangesAsync() => Task.FromResult(0);

        public Task BeginTransactionAsync() => Task.CompletedTask;

        public Task CommitAsync() => Task.CompletedTask;

        public Task RollbackAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }
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
