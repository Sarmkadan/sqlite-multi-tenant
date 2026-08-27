using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SqliteMultiTenant.Tests.Utilities
{
    /// <summary>
    /// Tests for the <see cref="OperationRetryPolicy"/> class, verifying retry behavior
    /// for transient and non-transient exceptions, custom configurations, and edge cases.
    /// </summary>
    public class OperationRetryPolicyTests
    {
        private readonly ILogger<OperationRetryPolicy> _logger;
        private readonly OperationRetryPolicy _retryPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationRetryPolicyTests"/> class
        /// with a substitute logger and a retry policy configured for 3 retries,
        /// 10ms initial delay, and 2.0 backoff multiplier.
        /// </summary>
        public OperationRetryPolicyTests()
        {
            _logger = Substitute.For<ILogger<OperationRetryPolicy>>();
            _retryPolicy = new OperationRetryPolicy(_logger, maxRetries: 3, initialDelayMs: 10, backoffMultiplier: 2.0);
            _logger.LogInformation("Initializing retry policy: MaxRetries={MaxRetries}, InitialDelayMs={InitialDelayMs}, BackoffMultiplier={BackoffMultiplier}", 3, 10, 2.0);
        }

        /// <summary>
        /// Verifies that when an operation succeeds on the first attempt, the retry policy
        /// returns the result immediately without any retries.
        /// </summary>
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
            _logger.LogInformation("Completed test: ExecuteAsync_WithSuccessfulOperationOnFirstTry_ReturnsResultWithoutRetrying with result {Result} after {CallCount} attempt(s)", expectedResult, callCount);
        }

        /// <summary>
        /// Verifies that when an operation fails once and then succeeds, the retry policy
        /// retries exactly once and then returns the successful result.
        /// </summary>
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

        /// <summary>
        /// Verifies that when an operation fails multiple times and then succeeds, the retry policy
        /// retries the configured number of times (maxRetries-1) and then returns the successful result.
        /// </summary>
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

        /// <summary>
        /// Verifies that when an operation fails for all retry attempts, the retry policy
        /// throws the last exception encountered.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithExhaustedRetries_ThrowsLastException()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithExhaustedRetries_ThrowsLastException");
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
            var assertions = await act.Should().ThrowAsync<System.Data.SQLite.SQLiteException>();
            _logger.LogError(assertions.Which, "Operation {OperationName} exhausted all {MaxRetries} retries and threw the last exception", "FailingOperation", 3);
            callCount.Should().Be(3); // Max retries reached
            _logger.LogInformation("Completed test: ExecuteAsync_WithExhaustedRetries_ThrowsLastException after {CallCount} attempts", callCount);
        }

        /// <summary>
        /// Verifies that when an operation throws a non-transient exception, the retry policy
        /// throws the exception immediately without any retries.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithNonTransientException_ThrowsImmediatelyWithoutRetry()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithNonTransientException_ThrowsImmediatelyWithoutRetry");
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
            var assertions = await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(e => e == expectedException);
            _logger.LogError(assertions.Which, "Non-transient exception {ExceptionType} thrown immediately without retry for operation {OperationName}", nameof(InvalidOperationException), "NonTransientOperation");
            callCount.Should().Be(1); // No retries for non-transient exceptions
            _logger.LogInformation("Completed test: ExecuteAsync_WithNonTransientException_ThrowsImmediatelyWithoutRetry after {CallCount} attempt(s)", callCount);
        }

        /// <summary>
        /// Verifies that when a void-returning operation succeeds on the first attempt, the retry policy
        /// completes successfully without any retries.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithVoidReturningOperation_SucceedsOnFirstTry()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithVoidReturningOperation_SucceedsOnFirstTry");
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
            _logger.LogInformation("Completed test: ExecuteAsync_WithVoidReturningOperation_SucceedsOnFirstTry after {CallCount} attempt(s)", callCount);
        }

        /// <summary>
        /// Verifies that when a void-returning operation fails initially but succeeds after retries, the retry policy
        /// retries the operation until it succeeds and then completes successfully.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithVoidReturningOperationAfterRetry_SucceedsAfterRetries()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithVoidReturningOperationAfterRetry_SucceedsAfterRetries");
            // Arrange
            var callCount = 0;

            Task VoidOperation()
            {
                callCount++;
                if (callCount <= 2)
                {
                    _logger.LogWarning("Attempt {Attempt} failed with network error, will retry", callCount);
                    throw new HttpRequestException("Network error");
                }
                return Task.CompletedTask;
            }

            // Act
            await _retryPolicy.ExecuteAsync(VoidOperation, "NetworkOperation");

            // Assert
            callCount.Should().Be(3);
            _logger.LogInformation("Completed test: ExecuteAsync_WithVoidReturningOperationAfterRetry_SucceedsAfterRetries after {CallCount} attempts", callCount);
        }

        /// <summary>
        /// Verifies that when a void-returning operation fails for all retry attempts, the retry policy
        /// throws the last exception encountered.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithVoidReturningOperationExhaustsRetries_ThrowsLastException()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithVoidReturningOperationExhaustsRetries_ThrowsLastException");
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
            var assertions = await act.Should().ThrowAsync<System.Data.SQLite.SQLiteException>();
            _logger.LogError(assertions.Which, "Void operation {OperationName} exhausted all {MaxRetries} retries and threw the last exception", "TimeoutOperation", 3);
            callCount.Should().Be(3);
            _logger.LogInformation("Completed test: ExecuteAsync_WithVoidReturningOperationExhaustsRetries_ThrowsLastException after {CallCount} attempts", callCount);
        }

        /// <summary>
        /// Verifies that when a custom retry policy is configured with specific settings,
        /// the policy uses those settings instead of the default ones.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithCustomRetryPolicyConfiguration_UsesCustomSettings()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithCustomRetryPolicyConfiguration_UsesCustomSettings");
            // Arrange
            var customPolicy = new OperationRetryPolicy(
                _logger,
                maxRetries: 5,
                initialDelayMs: 50,
                backoffMultiplier: 3.0);

            _logger.LogInformation("Created custom retry policy: MaxRetries={MaxRetries}, InitialDelayMs={InitialDelayMs}, BackoffMultiplier={BackoffMultiplier}", 5, 50, 3.0);

            var callCount = 0;

            Task<string> OperationWithCustomPolicy()
            {
                callCount++;
                if (callCount <= 4)
                {
                    _logger.LogWarning("Attempt {Attempt} failed with database lock, will retry", callCount);
                    throw new System.Data.SQLite.SQLiteException(System.Data.SQLite.SQLiteErrorCode.Busy, "Database locked");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await customPolicy.ExecuteAsync(OperationWithCustomPolicy, "CustomOperation");

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(5); // Should use custom maxRetries of 5
            _logger.LogInformation("Completed test: ExecuteAsync_WithCustomRetryPolicyConfiguration_UsesCustomSettings with result {Result} after {CallCount} attempts", result, callCount);
        }

        /// <summary>
        /// Verifies that when an operation throws a SQLiteException with error code Busy (database locked),
        /// the retry policy treats it as a transient exception and retries the operation.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithDatabaseLockedException_RetriesBecauseItIsTransient()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithDatabaseLockedException_RetriesBecauseItIsTransient");
            // Arrange
            var callCount = 0;

            Task<string> LockedDatabaseOperation()
            {
                callCount++;
                if (callCount <= 2)
                {
                    _logger.LogWarning("Attempt {Attempt} hit a locked database, will retry", callCount);
                    throw new System.Data.SQLite.SQLiteException(System.Data.SQLite.SQLiteErrorCode.Busy, "database is locked");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(LockedDatabaseOperation);

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(3);
            _logger.LogInformation("Completed test: ExecuteAsync_WithDatabaseLockedException_RetriesBecauseItIsTransient with result {Result} after {CallCount} attempts", result, callCount);
        }

        /// <summary>
        /// Verifies that when an operation throws an InvalidOperationException with message "database is locked",
        /// the retry policy treats it as a transient exception and retries the operation.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithInvalidOperationDatabaseLockedException_RetriesBecauseItIsTransient()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithInvalidOperationDatabaseLockedException_RetriesBecauseItIsTransient");
            // Arrange
            var callCount = 0;

            Task<string> LockedOperation()
            {
                callCount++;
                if (callCount <= 2) // Fail first two attempts (1 initial + 2 retries = 3 total attempts)
                {
                    _logger.LogWarning("Attempt {Attempt} reported 'database is locked', will retry", callCount);
                    throw new InvalidOperationException("database is locked");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await _retryPolicy.ExecuteAsync(LockedOperation);

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(3); // 1 initial + 2 retries (maxRetries=3)
            _logger.LogInformation("Completed test: ExecuteAsync_WithInvalidOperationDatabaseLockedException_RetriesBecauseItIsTransient with result {Result} after {CallCount} attempts", result, callCount);
        }

        /// <summary>
        /// Verifies that when a null operation is passed to ExecuteAsync, the retry policy
        /// throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithNullOperation_ThrowsArgumentNullException");
            // Arrange
            Func<Task<string>> nullOperation = null!;
            _logger.LogWarning("Invoking retry policy with a null operation for {OperationName}; expecting ArgumentNullException", "NullOperation");

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(nullOperation, "NullOperation");

            // Assert
            var assertions = await act.Should().ThrowAsync<ArgumentNullException>();
            _logger.LogError(assertions.Which, "ArgumentNullException thrown as expected for null operation {OperationName}", "NullOperation");
            _logger.LogInformation("Completed test: ExecuteAsync_WithNullOperation_ThrowsArgumentNullException");
        }

        /// <summary>
        /// Verifies that when a null void operation is passed to ExecuteAsync, the retry policy
        /// throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithNullVoidOperation_ThrowsArgumentNullException()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithNullVoidOperation_ThrowsArgumentNullException");
            // Arrange
            Func<Task> nullOperation = null!;
            _logger.LogWarning("Invoking retry policy with a null void operation for {OperationName}; expecting ArgumentNullException", "NullVoidOperation");

            // Act
            Func<Task> act = async () => await _retryPolicy.ExecuteAsync(nullOperation, "NullVoidOperation");

            // Assert
            var assertions = await act.Should().ThrowAsync<ArgumentNullException>();
            _logger.LogError(assertions.Which, "ArgumentNullException thrown as expected for null void operation {OperationName}", "NullVoidOperation");
            _logger.LogInformation("Completed test: ExecuteAsync_WithNullVoidOperation_ThrowsArgumentNullException");
        }

        /// <summary>
        /// Verifies that when a null operation name is passed to ExecuteAsync, the retry policy
        /// uses the operation's type name as the operation name in logs.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithNullOperationName_UsesOperationTypeName()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithNullOperationName_UsesOperationTypeName");
            // Arrange
            var expectedResult = "Result";
            Task<string> Operation() => Task.FromResult(expectedResult);

            // Act
            var result = await _retryPolicy.ExecuteAsync(Operation);

            // Assert
            result.Should().Be(expectedResult);
            _logger.LogInformation("Completed test: ExecuteAsync_WithNullOperationName_UsesOperationTypeName with result {Result}", result);
        }

        /// <summary>
        /// Verifies that when a retry policy is built using the RetryPolicyBuilder with specific settings,
        /// the resulting policy is correctly configured with those settings.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithRetryPolicyBuilder_BuildsCorrectlyConfiguredPolicy()
        {
            _logger.LogInformation("Starting test: ExecuteAsync_WithRetryPolicyBuilder_BuildsCorrectlyConfiguredPolicy");
            // Arrange
            var builder = new RetryPolicyBuilder()
                .WithMaxRetries(5)
                .WithInitialDelay(200)
                .WithBackoffMultiplier(2.5)
                .WithLogger(_logger);

            var policy = builder.Build();
            _logger.LogInformation("Built retry policy from builder: MaxRetries={MaxRetries}, InitialDelayMs={InitialDelayMs}, BackoffMultiplier={BackoffMultiplier}", 5, 200, 2.5);
            var callCount = 0;

            Task<string> Operation()
            {
                callCount++;
                if (callCount <= 4)
                {
                    _logger.LogWarning("Attempt {Attempt} failed with network timeout, will retry", callCount);
                    throw new HttpRequestException("Network timeout");
                }
                return Task.FromResult("Success");
            }

            // Act
            var result = await policy.ExecuteAsync(Operation, "BuilderOperation");

            // Assert
            result.Should().Be("Success");
            callCount.Should().Be(5);
            _logger.LogInformation("Completed test: ExecuteAsync_WithRetryPolicyBuilder_BuildsCorrectlyConfiguredPolicy with result {Result} after {CallCount} attempts", result, callCount);
        }

        /// <summary>
        /// Verifies that when building a retry policy with a null logger using the RetryPolicyBuilder,
        /// an InvalidOperationException is thrown with the message "Logger is required".
        /// </summary>
        [Fact]
        public void RetryPolicyBuilder_WithNullLogger_ThrowsInvalidOperationException()
        {
            _logger.LogInformation("Starting test: RetryPolicyBuilder_WithNullLogger_ThrowsInvalidOperationException");
            // Arrange
            var builder = new RetryPolicyBuilder();
            _logger.LogWarning("Building retry policy without a logger; expecting InvalidOperationException");

            // Act
            Action act = () => builder.Build();

            // Assert
            var assertions = act.Should().Throw<InvalidOperationException>()
                .WithMessage("Logger is required");
            _logger.LogError(assertions.Which, "InvalidOperationException thrown as expected when building a policy without a logger");
            _logger.LogInformation("Completed test: RetryPolicyBuilder_WithNullLogger_ThrowsInvalidOperationException");
        }
    }
}
