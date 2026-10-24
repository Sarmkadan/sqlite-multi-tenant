using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Operations;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Unit tests for <see cref="ConflictResolutionServiceExtensions"/>.
/// </summary>
public sealed class ConflictResolutionServiceExtensionsTests
{
    private readonly ConflictResolutionService _service;

    public ConflictResolutionServiceExtensionsTests()
    {
        var logger = Substitute.For<ILogger<ConflictResolutionService>>();
        _service = new ConflictResolutionService(logger);
    }

    // --- CreateConflictDetectionResult (single conflict) ---

    [Fact]
    public void CreateConflictDetectionResult_WithValidField_ReturnsSingleConflict()
    {
        // Arrange
        var field = "Name";
        var conflictType = ConflictType.ValueDifference;

        // Act
        var result = _service.CreateConflictDetectionResult(field, conflictType, "local", "remote");

        // Assert
        Assert.True(result.HasConflicts);
        Assert.Single(result.Conflicts);
        Assert.Equal(field, result.Conflicts[0].Field);
        Assert.Equal(conflictType, result.Conflicts[0].ConflictType);
        Assert.Equal("local", result.Conflicts[0].LocalValue);
        Assert.Equal("remote", result.Conflicts[0].RemoteValue);
    }

    [Fact]
    public void CreateConflictDetectionResult_WithNullField_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.CreateConflictDetectionResult(null!, ConflictType.ValueDifference, "a", "b"));
    }

    [Fact]
    public void CreateConflictDetectionResult_WithEmptyField_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateConflictDetectionResult("", ConflictType.ValueDifference, "a", "b"));
    }

    // --- CreateConflictDetectionResult (collection) ---

    [Fact]
    public void CreateConflictDetectionResult_WithCollection_PopulatesAll()
    {
        // Arrange
        var conflicts = new List<DataConflict>
        {
            new DataConflict { Field = "A", ConflictType = ConflictType.ValueDifference, LocalValue = 1, RemoteValue = 2 },
            new DataConflict { Field = "B", ConflictType = ConflictType.CreatedRemotely, LocalValue = null!, RemoteValue = "new" }
        };

        // Act
        var result = _service.CreateConflictDetectionResult(conflicts);

        // Assert
        Assert.Equal(2, result.Conflicts.Count);
        Assert.Equal("A", result.Conflicts[0].Field);
        Assert.Equal("B", result.Conflicts[1].Field);
    }

    [Fact]
    public void CreateConflictDetectionResult_WithNull_ReturnsEmpty()
    {
        // Act
        var result = _service.CreateConflictDetectionResult((IEnumerable<DataConflict>)null!);

        // Assert
        Assert.False(result.HasConflicts);
    }

    // --- GetConflictingFields ---

    [Fact]
    public void GetConflictingFields_DetectsValueDifference_ReturnsPreferLocal()
    {
        // Arrange
        var local = new Dictionary<string, object> { ["Name"] = "Alice" };
        var remote = new Dictionary<string, object> { ["Name"] = "Bob" };

        // Act
        var fields = _service.GetConflictingFields(local, remote);

        // Assert
        Assert.Single(fields);
        Assert.Equal(ConflictResolutionStrategy.PreferLocal, fields["Name"]);
    }

    [Fact]
    public void GetConflictingFields_WithNullLocal_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.GetConflictingFields(null!, new Dictionary<string, object>()));
    }

    // --- HasConflictType ---

    [Fact]
    public void HasConflictType_WhenTypePresent_ReturnsTrue()
    {
        // Arrange
        var detection = new ConflictDetectionResult();
        detection.AddConflict(new DataConflict
        {
            Field = "X",
            ConflictType = ConflictType.ModifiedBoth,
            LocalValue = 1,
            RemoteValue = 2
        });

        // Act
        var has = _service.HasConflictType(detection, ConflictType.ModifiedBoth);

        // Assert
        Assert.True(has);
    }

    [Fact]
    public void HasConflictType_WhenTypeMissing_ReturnsFalse()
    {
        // Arrange
        var detection = new ConflictDetectionResult();

        // Act & Assert
        Assert.False(_service.HasConflictType(detection, ConflictType.CreatedRemotely));
    }

    [Fact]
    public void HasConflictType_WithNullDetection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.HasConflictType(null!, ConflictType.ValueDifference));
    }

    // --- GetFirstConflictOfType ---

    [Fact]
    public void GetFirstConflictOfType_WhenPresent_ReturnsFirst()
    {
        // Arrange
        var detection = new ConflictDetectionResult();
        detection.AddConflict(new DataConflict { Field = "A", ConflictType = ConflictType.ValueDifference });
        detection.AddConflict(new DataConflict { Field = "B", ConflictType = ConflictType.ValueDifference });

        // Act
        var conflict = _service.GetFirstConflictOfType(detection, ConflictType.ValueDifference);

        // Assert
        Assert.NotNull(conflict);
        Assert.Equal("A", conflict!.Field);
    }

    [Fact]
    public void GetFirstConflictOfType_WhenMissing_ReturnsNull()
    {
        // Arrange
        var detection = new ConflictDetectionResult();

        // Act
        var conflict = _service.GetFirstConflictOfType(detection, ConflictType.DeletedRemotely);

        // Assert
        Assert.Null(conflict);
    }

    // --- ResolveConflictsAsync ---

    [Fact]
    public async Task ResolveConflictsAsync_WithCustomResolver_ResolvesAll()
    {
        // Arrange
        var detection = new ConflictDetectionResult();
        detection.AddConflict(new DataConflict
        {
            Field = "Price",
            ConflictType = ConflictType.ValueDifference,
            LocalValue = 10,
            RemoteValue = 20
        });
        Func<string, ConflictType, object?, object?, object?> resolver =
            (field, type, local, remote) => remote; // always prefer remote

        // Act
        var result = await _service.ResolveConflictsAsync(detection, resolver);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(20, result.ResolvedValues["Price"]);
    }

    [Fact]
    public async Task ResolveConflictsAsync_WithNullResolver_ThrowsArgumentNullException()
    {
        // Arrange
        var detection = new ConflictDetectionResult();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.ResolveConflictsAsync(detection, null!));
    }

    [Fact]
    public async Task ResolveConflictsAsync_WithThrowingResolver_CapturesError()
    {
        // Arrange
        var detection = new ConflictDetectionResult();
        detection.AddConflict(new DataConflict
        {
            Field = "F",
            ConflictType = ConflictType.ValueDifference,
            LocalValue = "a",
            RemoteValue = "b"
        });
        Func<string, ConflictType, object?, object?, object?> resolver =
            (_, _, _, _) => throw new InvalidOperationException("boom");

        // Act
        var result = await _service.ResolveConflictsAsync(detection, resolver);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("boom", result.Error);
    }
}
