using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SqliteMultiTenant.Tests.Utilities
{
    public class OperationRetryPolicyTests
    {
        private readonly ILogger<OperationRetryPolicy> _logger;
        private readonly OperationRetryPolicy _retryPolicy;

        public OperationRetryPolicyTests()
        {
            _logger = Substitute.For<ILogger<OperationRetryPolicy>>();
            _logger.LogInformation("Initializing retry policy: MaxRetries={MaxRetries}, InitialDelayMs={InitialDelayMs}, BackoffMultiplier={BackoffMultiplier}", _retryPolicy.MaxRetries, _retryPolicy.InitialDelayMs, _retryPolicy.BackoffMultiplier);
            _retryPolicy = new OperationRetryPolicy(_logger, maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperationOnFirstTry_ReturnsResultWithoutRetrying()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithSuccessfulOperationOnFirstTry_ReturnsResultWithoutRetrying");
            // Arrange
            var expectedResult = "Success";
            var callCount = 0;

            Task<string> SuccessfulOperation()
            {
                callCount++;
                return Task.FromResult(expectedResult);
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(SuccessfulOperation, "TestOperation");

            // Assert
            result.Should().Be(expectedResult);
            callCount.Should().Be(1);
            _logger.LogInformation("Completed test: ExecuteAsync_WithSuccessfulOperationOnFirstTry_ReturnsResultWithoutRetrying with result {Result}", expectedResult);
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperationAfterOneRetry_RetriesOnceThenSucceeds()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithSuccessfulOperationAfterOneRetry_RetriesOnceThenSucceeds");
            // Arrange
            var expectedResult = "Success";
            var callCount = 0;

            Task<string> OperationWithOneTransientFailure()
            {
                callCount++;
                if (callCount <= 2) // Fail first two attempts
                {
                    _logger.LogWarning("Attempt {Attempt} failed with timeout, will retry", callCount);
                    throw new TimeoutException("Simulated timeout");
                }
                _logger.LogInformation("Attempt {Attempt} succeeded", callCount);
                return Task.FromResult(expectedResult);
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(OperationWithOneTransientFailure, "TestOperation");

            // Assert
            result.Should().Be(expectedResult);
            callCount.Should().Be(3); // 1 initial + 2 retries
            _logger.LogInformation("Completed test: ExecuteAsync_WithSuccessfulOperationAfterOneRetry_RetriesOnceThenSucceeds with result {Result} after {CallCount} attempts", expectedResult, callCount);
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperationAfterMultipleRetries_RetriesMultipleTimesThenSucceeds()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithSuccessfulOperationAfterMultipleRetries_RetriesMultipleTimesThenSucceeds");
            // Arrange
            var expectedResult = 42;
            var callCount = 0;

            Task<int> OperationWithMultipleTransientFailures()
            {
                callCount++;
                if (callCount <= 2) // Fail first two attempts (1 initial + 2 retries = 3 total attempts)
                {
                    _logger.LogWarning("Attempt {Attempt} failed with I/O error, will retry", callCount);
                    throw new System.IO.IOException("Simulated I/O error");
                }
                _logger.LogInformation("Attempt {Attempt} succeeded", callCount);
                return Task.FromResult(expectedResult);
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(OperationWithMultipleTransientFailures, "DatabaseOperation");

            // Assert
            result.Should().Be(expectedResult);
            callCount.Should().Be(3); // 1 initial + 2 retries (maxRetries=3)
            _logger.LogInformation("Completed test: ExecuteAsync_WithSuccessfulOperationAfterMultipleRetries_RetriesMultipleTimesThenSucceeds with result {Result} after {CallCount} attempts", expectedResult, callCount);
        }

        [Fact]
        public async Task ExecuteAsync_WithExhaustedRetries_ThrowsLastException()
        {
            // Arrange
            var callCount = 0;
            var expectedException = new System.Data.SQLite.SQLiteException(System.Data.SQLite.SQLiteErrorCode.Busy, "Database locked");

            Task<string> AlwaysFailingOperation()
            {
                callCount++;
                throw expectedException;
            }

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(AlwaysFailingOperation, "FailingOperation");

            // Assert
            await act.Should().ThrowAsync<System.Data.SQLite.SQLiteException>();
            callCount.Should().Be(3); // Max retries reached
        }

        [Fact]
        public async Task ExecuteAsync_WithNonTransientException_ThrowsImmediatelyWithoutRetry()
        {
            // Arrange
            var callCount = 0;
            var expectedException = new InvalidOperationException("Permanent failure");

            Task<string> NonTransientOperation()
            {
                callCount++;
                throw expectedException;
            }

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(NonTransientOperation, "NonTransientOperation");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(e => e == expectedException);
            callCount.Should().Be(1); // No retries for non-transient exceptions
        }

        [Fact]
        public async Task ExecuteAsync_WithVoidReturningOperation_SucceedsOnFirstTry()
        {
            // Arrange
            var callCount = 0;

            Task VoidOperation()
            {
                callCount++;
                return Task.CompletedTask;
            }

            // Act
            await _retryPolicy.ExecuteAsync(VoidOperation, "VoidOperation");

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_WithVoidReturningOperationAfterRetry_SucceedsAfterRetries()
        {
            // Arrange
            var callCount = 0;

            Task VoidOperation()
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new HttpRequestException("Network error");
                }
                return Task.CompletedTask;
            }

            // Act
            await _retryPolicy.ExecuteAsync(VoidOperation, "NetworkOperation");

            // Assert
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_WithVoidReturningOperationExhaustsRetries_ThrowsLastException()
        {
            // Arrange
            var callCount = 0;
            var expectedException = new System.Data.SQLite.SQLiteException(System.Data.SQLite.SQLiteErrorCode.Busy, "Database locked");

            Task AlwaysFailingVoidOperation()
            {
                callCount++;
                throw expectedException;
            }

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(AlwaysFailingVoidOperation, "TimeoutOperation");

            // Assert
            await act.Should().ThrowAsync<System.Data.SQLite.SQLiteException>();
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_WithCustomRetryPolicyConfiguration_UsesCustomSettings()
        {
            // Arrange
            var customPolicy = new OperationRetryPolicy(
                _logger,
                maxRetries: 5,
                initialDelayMs: 50,
                backoffMultiplier: 3.0);

            var callCount = 0;

            Task<string> OperationWithCustomPolicy()
            {
                callCount++;
                if (callCount <= 4)
                {
                    throw new System.Data.SQLite.SQLiteException(System.Data.SQLite.SQLiteErrorCode.Busy, "Database locked");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await customPolicy.ExecuteAsync(OperationWithCustomPolicy, "CustomOperation");

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(5); // Should use custom maxRetries of 5
        }

        [Fact]
        public async Task ExecuteAsync_WithDatabaseLockedException_RetriesBecauseItIsTransient()
        {
            // Arrange
            var callCount = 0;

            Task<string> LockedDatabaseOperation()
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new System.Data.SQLite.SQLiteException(System.Data.SQLite.SQLiteErrorCode.Busy, "database is locked");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(LockedDatabaseOperation);

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidOperationDatabaseLockedException_RetriesBecauseItIsTransient()
        {
            // Arrange
            var callCount = 0;

            Task<string> LockedOperation()
            {
                callCount++;
                if (callCount <= 2) // Fail first two attempts (1 initial + 2 retries = 3 total attempts)
                {
                    throw new InvalidOperationException("database is locked");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(LockedOperation);

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(3); // 1 initial + 2 retries (maxRetries=3)
        }

        [Fact]
        public async Task ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
        {
            // Arrange
            Func<Task<string>> nullOperation = null!;

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(nullOperation, "NullOperation");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullVoidOperation_ThrowsArgumentNullException()
        {
            // Arrange
            Func<Task> nullOperation = null!;

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(nullOperation, "NullVoidOperation");

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullOperationName_UsesOperationTypeName()
        {
            // Arrange
            var expectedResult = "Result";
            Task<string> Operation() => Task.FromResult(expectedResult);

            // Act
            var result = await _retryPolicy.ExecuteAsync(Operation);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ExecuteAsync_WithRetryPolicyBuilder_BuildsCorrectlyConfiguredPolicy()
        {
            // Arrange
            var builder = new RetryPolicyBuilder()
                .WithMaxRetries(5)
                .WithInitialDelay(200)
                .WithBackoffMultiplier(2.5)
                .WithLogger(_logger);

            var policy = builder.Build();
            var callCount = 0;

            Task<string> Operation()
            {
                callCount++;
                if (callCount <= 4)
                {
                    throw new HttpRequestException("Network timeout");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await policy.ExecuteAsync(Operation, "BuilderOperation");

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(5);
        }

        [Fact]
        public void RetryPolicyBuilder_WithNullLogger_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = new RetryPolicyBuilder();

            // Act
            Action act = () => builder.Build();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Logger is required");
        }
    }
}
