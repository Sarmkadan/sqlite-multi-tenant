using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SqliteMultiTenant.Repositories;
using Xunit;

namespace SqliteMultiTenant.Tests;

public sealed class GenericRepositoryJsonExtensionsTests
{
    private class TestEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestGenericRepository : GenericRepository<TestEntity>
    {
        public List<TestEntity> Items { get; } = new();

        public TestGenericRepository() : base(NullLogger.Instance) { }

        public override Task<List<TestEntity>> GetAllAsync() => Task.FromResult(Items);
        public override Task<TestEntity?> GetByIdAsync(string id) => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
        public override Task<TestEntity> CreateAsync(TestEntity entity)
        {
            Items.Add(entity);
            return Task.FromResult(entity);
        }
        public override Task<bool> UpdateAsync(TestEntity entity) => Task.FromResult(true);
        public override Task<bool> DeleteAsync(string id) => Task.FromResult(true);
        public override Task<List<TestEntity>> FindAsync(Func<TestEntity, bool> predicate) => Task.FromResult(Items.Where(predicate).ToList());
        public override Task<int> GetCountAsync() => Task.FromResult(Items.Count);
        public override Task<bool> ExistsAsync(string id) => Task.FromResult(true);
        public override Task<int> DeleteAsync(Func<TestEntity, bool> predicate) => Task.FromResult(0);
        public override Task<PagedResult<TestEntity>> GetPageAsync(string tenantId, int pageNumber, int pageSize, string? orderBy = null) 
            => Task.FromResult(new PagedResult<TestEntity>(new List<TestEntity>(), 0, 1, 10, 0));
        public Task<int> SaveChangesAsync() => Task.FromResult(0);
        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task CommitAsync() => Task.CompletedTask;
        public Task RollbackAsync() => Task.CompletedTask;
        public void Dispose() { }
    }

    [Fact]
    public void ToJson_ValidRepository_ReturnsEmptyJsonBecauseBaseClassHasNoPublicProperties()
    {
        // Arrange
        var repository = new TestGenericRepository();
        repository.Items.Add(new TestEntity { Id = "1", Name = "Test" });

        // Act
        var json = repository.ToJson();

        // Assert
        json.Should().Be("{}");
    }

    [Fact]
    public void ToJson_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        TestGenericRepository? repository = null;

        // Act
        var act = () => repository!.ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var act = () => GenericRepositoryJsonExtensions.FromJson<TestEntity>(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var result = GenericRepositoryJsonExtensions.TryFromJson<TestEntity>(json, out var repository);

        // Assert
        result.Should().BeFalse();
        repository.Should().BeNull();
    }
}
