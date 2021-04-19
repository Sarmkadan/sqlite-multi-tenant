// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Utilities
{
    // Implements exponential backoff retry logic for transient failures
    public class OperationRetryPolicy
    {
        private readonly ILogger<OperationRetryPolicy> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly double _backoffMultiplier;

        public OperationRetryPolicy(ILogger<OperationRetryPolicy> logger,
            int maxRetries = 3, int initialDelayMs = 100, double backoffMultiplier = 2.0)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRetries = maxRetries;
            _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _backoffMultiplier = backoffMultiplier;
        }

        // Executes an operation with retry logic
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = null)
        {
            if (operation == null)
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

        // Executes an operation without return value
        public async Task ExecuteAsync(Func<Task> operation, string operationName = null)
        {
            if (operation == null)
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

        // Determines if an exception is a transient failure (retryable)
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

    // Builder for configurable retry policies
    public class RetryPolicyBuilder
    {
        private int _maxRetries = 3;
        private int _initialDelayMs = 100;
        private double _backoffMultiplier = 2.0;
        private ILogger _logger;

        public RetryPolicyBuilder WithMaxRetries(int maxRetries)
        {
            _maxRetries = maxRetries;
            return this;
        }

        public RetryPolicyBuilder WithInitialDelay(int delayMs)
        {
            _initialDelayMs = delayMs;
            return this;
        }

        public RetryPolicyBuilder WithBackoffMultiplier(double multiplier)
        {
            _backoffMultiplier = multiplier;
            return this;
        }

        public RetryPolicyBuilder WithLogger(ILogger logger)
        {
            _logger = logger;
            return this;
        }

        public OperationRetryPolicy Build()
        {
            if (_logger == null)
                throw new InvalidOperationException("Logger is required");

            return new OperationRetryPolicy(_logger, _maxRetries, _initialDelayMs, _backoffMultiplier);
        }
    }
}
