#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Utilities
{
    /// <summary>
    /// Implements exponential backoff retry logic for transient failures.
    /// </summary>
    public sealed class OperationRetryPolicy
    {
        private readonly ILogger<OperationRetryPolicy> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationRetryPolicy"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="initialDelayMs">The initial delay in milliseconds.</param>
        /// <param name="backoffMultiplier">The backoff multiplier.</param>
        public OperationRetryPolicy(ILogger<OperationRetryPolicy> logger,
            int maxRetries = 3, int initialDelayMs = 100, double backoffMultiplier = 2.0)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRetries = maxRetries;
            _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _backoffMultiplier = backoffMultiplier;
        }

        /// <summary>
        /// Executes an operation with retry logic.
        /// </summary>
        /// <typeparam name="T">The type of the operation result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <returns>The result of the operation.</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = null)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            var opName = operationName ?? operation.GetType().Name;
            var lastException = default(Exception);
            var delay = _initialDelay;

            for (int attempt = 0; attempt < _maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Executing operation '{OperationName}' - Attempt {Attempt}/{MaxRetries}",
                        opName, attempt + 1, _maxRetries);

                    return await operation();
                }
                catch (Exception ex) when (IsTransientFailure(ex))
                {
                    lastException = ex;

                    if (attempt < _maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "Operation '{OperationName}' failed - Retrying after {DelayMs}ms",
                            opName, delay.TotalMilliseconds);

                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _backoffMultiplier);
                    }
                }
            }

            _logger.LogError(lastException, "Operation '{OperationName}' failed after {MaxRetries} retries",
                opName, _maxRetries);

            throw lastException ?? new InvalidOperationException(
                $"Operation '{opName}' failed after {_maxRetries} retries");
        }

        /// <summary>
        /// Executes an operation without return value.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation.</param>
        public async Task ExecuteAsync(Func<Task> operation, string operationName = null)
        {
            if (operation is null)
                throw new ArgumentNullException(nameof(operation));

            var opName = operationName ?? operation.GetType().Name;
            var lastException = default(Exception);
            var delay = _initialDelay;

            for (int attempt = 0; attempt < _maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Executing operation '{OperationName}' - Attempt {Attempt}/{MaxRetries}",
                        opName, attempt + 1, _maxRetries);

                    await operation();
                    return;
                }
                catch (Exception ex) when (IsTransientFailure(ex))
                {
                    lastException = ex;

                    if (attempt < _maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "Operation '{OperationName}' failed - Retrying after {DelayMs}ms",
                            opName, delay.TotalMilliseconds);

                        await Task.Delay(delay);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * _backoffMultiplier);
                    }
                }
            }

            _logger.LogError(lastException, "Operation '{OperationName}' failed after {MaxRetries} retries",
                opName, _maxRetries);

            throw lastException ?? new InvalidOperationException(
                $"Operation '{opName}' failed after {_maxRetries} retries");
        }

        /// <summary>
        /// Determines if an exception is a transient failure (retryable).
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns><c>true</c> if the exception is a transient failure; otherwise, <c>false</c>.</returns>
        private bool IsTransientFailure(Exception ex)
        {
            // Common transient failures
            return ex switch
            {
                // Timeout
                TimeoutException => true,

                // I/O errors
                System.IO.IOException => true,

                // Database locked or unavailable
                System.Data.SQLite.SQLiteException sqlex when
                    sqlex.ErrorCode == 5 || // Database is locked
                    sqlex.ErrorCode == 17 => true, // I/O error

                // Network errors
                System.Net.Http.HttpRequestException => true,

                // Temporary failure
                InvalidOperationException ioe when
                    ioe.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase) => true,

                _ => false
            };
        }
    }

    /// <summary>
    /// Builder for configurable retry policies.
    /// </summary>
    public sealed class RetryPolicyBuilder
    {
        private int _maxRetries = 3;
        private int _initialDelayMs = 100;
        private double _backoffMultiplier = 2.0;
        private ILogger<OperationRetryPolicy> _logger;

        /// <summary>
        /// Sets the maximum number of retries.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <returns>The builder instance.</returns>
        public RetryPolicyBuilder WithMaxRetries(int maxRetries)
        {
            _maxRetries = maxRetries;
            return this;
        }

        /// <summary>
        /// Sets the initial delay in milliseconds.
        /// </summary>
        /// <param name="delayMs">The initial delay in milliseconds.</param>
        /// <returns>The builder instance.</returns>
        public RetryPolicyBuilder WithInitialDelay(int delayMs)
        {
            _initialDelayMs = delayMs;
            return this;
        }

        /// <summary>
        /// Sets the backoff multiplier.
        /// </summary>
        /// <param name="multiplier">The backoff multiplier.</param>
        /// <returns>The builder instance.</returns>
        public RetryPolicyBuilder WithBackoffMultiplier(double multiplier)
        {
            _backoffMultiplier = multiplier;
            return this;
        }

        /// <summary>
        /// Sets the logger instance.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <returns>The builder instance.</returns>
        public RetryPolicyBuilder WithLogger(ILogger<OperationRetryPolicy> logger)
        {
            _logger = logger;
            return this;
        }

        /// <summary>
        /// Builds the retry policy instance.
        /// </summary>
        /// <returns>The retry policy instance.</returns>
        public OperationRetryPolicy Build()
        {
            if (_logger is null)
                throw new InvalidOperationException("Logger is required");

            return new OperationRetryPolicy(_logger, _maxRetries, _initialDelayMs, _backoffMultiplier);
        }
    }
}
