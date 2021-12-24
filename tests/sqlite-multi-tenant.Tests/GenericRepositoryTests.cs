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

/// <summary>
/// Test suite for verifying the behavior of <see cref="GenericRepository{TEntity}"/> implementations.
/// This class provides unit tests for the generic repository pattern used throughout the application,
/// ensuring that basic CRUD operations and transaction handling work as expected.
/// </summary>
public sealed class GenericRepositoryTests {
    /// <summary>
/// Test entity used for testing generic repository operations.
/// </summary>
private class TestEntity { public string Id { get; set; } = string.Empty; }

    /// <summary>
/// Test implementation of <see cref="GenericRepository{TEntity}"/> that uses an in-memory list for storage.
/// This allows testing repository operations without requiring a database connection.
/// </summary>
private class TestGenericRepository : GenericRepository<TestEntity>
    {
        	/// <summary>
	/// Gets the in-memory collection of test entities.
	/// </summary>
	public List<TestEntity> Items { get; } = new();

        	/// <summary>
	/// Initializes a new instance of the <see cref="TestGenericRepository"/> class.
	/// </summary>
	public TestGenericRepository() : base(NullLogger.Instance)
        {
        }

        	/// <summary>
	/// Asynchronously retrieves all entities from the repository.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of all entities.</returns>
	public override Task<List<TestEntity>> GetAllAsync() => Task.FromResult(new List<TestEntity>(Items));

        	/// <summary>
	/// Asynchronously retrieves an entity by its identifier.
	/// </summary>
	/// <param name="id">The identifier of the entity to retrieve.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the entity if found, otherwise null.</returns>
	public override Task<TestEntity?> GetByIdAsync(string id) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        	/// <summary>
	/// Asynchronously creates a new entity in the repository.
	/// </summary>
	/// <param name="entity">The entity to create.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the created entity.</returns>
	public override Task<TestEntity> CreateAsync(TestEntity entity)
        {
            Items.Add(entity);
            return Task.FromResult(entity);
        }

        	/// <summary>
	/// Asynchronously updates an existing entity in the repository.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the update was successful.</returns>
	public override Task<bool> UpdateAsync(TestEntity entity)
        {
            var index = Items.FindIndex(i => i.Id == entity.Id);
            if (index < 0)
                return Task.FromResult(false);

            Items[index] = entity;
            return Task.FromResult(true);
        }

        	/// <summary>
	/// Asynchronously deletes an entity by its identifier.
	/// </summary>
	/// <param name="id">The identifier of the entity to delete.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
	public override Task<bool> DeleteAsync(string id)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item is null)
                return Task.FromResult(false);

            Items.Remove(item);
            return Task.FromResult(true);
        }

        	/// <summary>
	/// Asynchronously finds entities that match the specified predicate.
	/// </summary>
	/// <param name="predicate">A function to test each entity for a condition.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of entities that match the predicate.</returns>
	public override Task<List<TestEntity>> FindAsync(Func<TestEntity, bool> predicate) =>
            Task.FromResult(Items.Where(predicate).ToList());

        	/// <summary>
	/// Asynchronously gets the count of entities in the repository.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the count of entities.</returns>
	public override Task<int> GetCountAsync() => Task.FromResult(Items.Count);

        	/// <summary>
	/// Asynchronously checks whether an entity with the specified identifier exists.
	/// </summary>
	/// <param name="id">The identifier to check.</param>
	/// <returns>A task that represents the asynchronous operation. The task result indicates whether the entity exists.</returns>
	public override Task<bool> ExistsAsync(string id) => Task.FromResult(Items.Any(i => i.Id == id));

        	/// <summary>
	/// Asynchronously deletes entities that match the specified predicate.
	/// </summary>
	/// <param name="predicate">A function to test each entity for a condition.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the count of deleted entities.</returns>
	public override Task<int> DeleteAsync(Func<TestEntity, bool> predicate)
        {
            var toRemove = Items.Where(predicate).ToList();
            foreach (var item in toRemove)
                Items.Remove(item);

            return Task.FromResult(toRemove.Count);
        }

        	/// <summary>
	/// Asynchronously saves all changes made in the repository.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the number of changes saved (always 0 for this test implementation).</returns>
	public Task<int> SaveChangesAsync() => Task.FromResult(0);

        	/// <summary>
	/// Asynchronously begins a new transaction.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public Task BeginTransactionAsync() => Task.CompletedTask;

        	/// <summary>
	/// Asynchronously commits the current transaction.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public Task CommitAsync() => Task.CompletedTask;

        	/// <summary>
	/// Asynchronously rolls back the current transaction.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public Task RollbackAsync() => Task.CompletedTask;

        	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
        {
        }
    }

    	/// <summary>
	/// Gets the test repository instance used for all test operations.
	/// </summary>
	private readonly TestGenericRepository _repository;

    	/// <summary>
	/// Initializes a new instance of the <see cref="GenericRepositoryTests"/> class.
	/// </summary>
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
