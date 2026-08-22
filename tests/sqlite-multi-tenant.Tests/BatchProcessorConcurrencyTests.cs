using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Operations;
using Xunit;

namespace SqliteMultiTenant.Tests;

/// <summary>
/// Concurrency tests for BatchProcessor to ensure thread-safe operation
/// </summary>
public class BatchProcessorConcurrencyTests
{
    private readonly ILogger<BatchProcessor> _mockLogger;
    private readonly BatchProcessor _batchProcessor;

    public BatchProcessorConcurrencyTests()
    {
        _mockLogger = Substitute.For<ILogger<BatchProcessor>>();
        _batchProcessor = new BatchProcessor(_mockLogger);
    }

    [Fact]
    public async Task ProcessAsync_WithManyItems_ShouldPreserveAllItems()
    {
        // Arrange
        _mockLogger.LogInformation("Starting test {TestName}", nameof(ProcessAsync_WithManyItems_ShouldPreserveAllItems));
        var itemCount = 1000;
        var items = Enumerable.Range(0, itemCount).Select(i => $"item_{i}").ToList();

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1); // Simulate async work
                return item.ToUpper();
            },
            maxConcurrency: 10
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount);
        result.ErrorCount.Should().Be(0);
        result.SuccessfulResults.Should().HaveCount(itemCount);

        _mockLogger.LogInformation("Completed test {TestName} with {SuccessCount} successes and {ErrorCount} errors",
            nameof(ProcessAsync_WithManyItems_ShouldPreserveAllItems), result.SuccessCount, result.ErrorCount);
    }

    [Fact]
    public async Task ProcessAsync_WithHighConcurrency_ShouldNotLoseItems()
    {
        // Arrange
        var itemCount = 100;
        var concurrencyLevels = new[] { 1, 5, 10 };
        var random = new Random();

        foreach (var concurrencyLevel in concurrencyLevels)
        {
            var items = Enumerable.Range(0, itemCount).Select(i => i).ToList();
            var concurrency = concurrencyLevel; // Local variable for closure

            // Act
            var result = await _batchProcessor.ProcessAsync(
                items,
                async item =>
                {
                    // Simulate variable work duration
                    await Task.Delay(random.Next(1, 3));
                    return item * 2;
                },
                maxConcurrency: concurrency
            );

            // Assert
            result.TotalCount.Should().Be(itemCount, $"Concurrency: {concurrency}");
            result.SuccessCount.Should().Be(itemCount, $"Concurrency: {concurrency}");
            result.ErrorCount.Should().Be(0, $"Concurrency: {concurrency}");
        }
    }

    [Fact]
    public async Task ProcessAsync_WithExceptionInOneBatch_ShouldNotCorruptOtherBatches()
    {
        // Arrange
        var itemCount = 100;
        var items = Enumerable.Range(0, itemCount).ToList();
        var failedItemIndex = 50; // The item that will fail

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1); // Simulate async work

                if (item == failedItemIndex)
                {
                    throw new InvalidOperationException("Simulated failure for testing error isolation");
                }

                return item;
            },
            maxConcurrency: 10
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount - 1);
        result.ErrorCount.Should().Be(1);
        result.SuccessfulResults.Should().HaveCount(itemCount - 1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ItemId.Should().Be(failedItemIndex.ToString());
        result.Errors[0].Exception.Should().Contain("InvalidOperationException");
        result.Errors[0].Message.Should().Contain("Simulated failure");
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleExceptions_ShouldIsolateErrors()
    {
        // Arrange
        var itemCount = 200;
        var items = Enumerable.Range(0, itemCount).ToList();
        var failedIndices = new[] { 25, 75, 125, 175 };

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1); // Simulate async work

                if (failedIndices.Contains(item))
                {
                    throw new InvalidOperationException($"Simulated failure for item {item}");
                }

                return item;
            },
            maxConcurrency: 15
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount - failedIndices.Length);
        result.ErrorCount.Should().Be(failedIndices.Length);
        result.SuccessfulResults.Should().HaveCount(itemCount - failedIndices.Length);
        result.Errors.Should().HaveCount(failedIndices.Length);

        // Verify each failed item has its own error entry
        foreach (var failedIndex in failedIndices)
        {
            var error = result.Errors.FirstOrDefault(e => e.ItemId == failedIndex.ToString());
            error.Should().NotBeNull($"Error for item {failedIndex} should exist");
            error!.Exception.Should().Contain("InvalidOperationException");
            error.Message.Should().Contain(failedIndex.ToString());
        }
    }

    [Fact]
    public async Task ProcessAsync_WithNoConcurrency_ShouldProcessSequentially()
    {
        // Arrange
        var itemCount = 50;
        var items = Enumerable.Range(0, itemCount).ToList();
        var processedItems = new ConcurrentBag<int>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(10); // Simulate async work
                processedItems.Add(item);
                return item;
            },
            maxConcurrency: 1
        );

        stopwatch.Stop();

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount);
        result.ErrorCount.Should().Be(0);
        processedItems.Count.Should().Be(itemCount);
        processedItems.Should().BeEquivalentTo(items);

        // With maxConcurrency=1, processing should take roughly itemCount * delay time
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(itemCount * 8);
    }

    [Fact]
    public async Task ProcessAsync_WithoutResult_ShouldHandleCorrectly()
    {
        // Arrange
        var itemCount = 200;
        var items = Enumerable.Range(0, itemCount).ToList();

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1); // Simulate async work
            },
            maxConcurrency: 8
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount);
        result.ErrorCount.Should().Be(0);
        result.SuccessfulResults.Should().HaveCount(itemCount);
    }

    [Fact]
    public async Task ProcessAsync_WithExceptionWithoutResult_ShouldIsolateErrors()
    {
        // Arrange
        var itemCount = 150;
        var items = Enumerable.Range(0, itemCount).ToList();
        var failedItemIndex = 75;

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1); // Simulate async work

                if (item == failedItemIndex)
                {
                    throw new ArgumentException("Test exception without result");
                }
            },
            maxConcurrency: 12
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount - 1);
        result.ErrorCount.Should().Be(1);
        result.SuccessfulResults.Should().HaveCount(itemCount - 1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ItemId.Should().Be(failedItemIndex.ToString());
        result.Errors[0].Exception.Should().Contain("ArgumentException");
    }

    [Fact]
    public async Task ProcessAsync_WithVeryHighConcurrency_ShouldHandleGracefully()
    {
        // Arrange
        var itemCount = 200;
        var items = Enumerable.Range(0, itemCount).Select(i => $"item_{i}").ToList();

        // Act - Use very high concurrency to stress test
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1); // Simulate async work
                return item.ToUpper();
            },
            maxConcurrency: 50
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount);
        result.ErrorCount.Should().Be(0);
        result.SuccessfulResults.Should().HaveCount(itemCount);
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyCollection_ShouldReturnEmptyResult()
    {
        // Arrange
        var items = Array.Empty<string>();

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1);
                return item.ToUpper();
            }
        );

        // Assert
        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.SuccessfulResults.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WithAllItemsFailing_ShouldPreserveAllErrors()
    {
        // Arrange
        var itemCount = 100;
        var items = Enumerable.Range(0, itemCount).ToList();

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("All items should fail");
            },
            maxConcurrency: 20
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(0);
        result.ErrorCount.Should().Be(itemCount);
        result.SuccessfulResults.Should().BeEmpty();
        result.Errors.Should().HaveCount(itemCount);

        // Verify all items have errors
        foreach (var error in result.Errors)
        {
            error.Exception.Should().Contain("InvalidOperationException");
            error.Message.Should().Contain("All items should fail");
        }
    }

    [Fact]
    public async Task ProcessAsync_WithMixedSuccessAndFailure_ShouldCalculateCorrectStats()
    {
        // Arrange
        var itemCount = 200;
        var items = Enumerable.Range(0, itemCount).ToList();
        var failedIndices = new[] { 10, 50, 100, 150, 190 };

        // Act
        var result = await _batchProcessor.ProcessAsync(
            items,
            async item =>
            {
                await Task.Delay(1);
                if (failedIndices.Contains(item))
                {
                    throw new InvalidOperationException($"Item {item} failed");
                }
                return item * 2;
            },
            maxConcurrency: 10
        );

        // Assert
        result.TotalCount.Should().Be(itemCount);
        result.SuccessCount.Should().Be(itemCount - failedIndices.Length);
        result.ErrorCount.Should().Be(failedIndices.Length);
        result.SuccessRate.Should().Be((double)(itemCount - failedIndices.Length) / itemCount);

        // Verify success rate calculation
        var expectedSuccessRate = (double)(itemCount - failedIndices.Length) / itemCount;
        result.SuccessRate.Should().BeApproximately(expectedSuccessRate, 0.001);
    }
}