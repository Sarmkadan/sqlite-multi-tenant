using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using SqliteMultiTenant.Middleware;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class RateLimitingMiddlewareValidationTests
    {
        private static RateLimitingOptions CreateValidOptions()
        {
            return new RateLimitingOptions
            {
                RequestsPerMinute = 60,
                BurstCapacity = 10,
                CleanupIntervalSeconds = 30
            };
        }

        private static RateLimitingOptions CreateInvalidOptions()
        {
            return new RateLimitingOptions
            {
                RequestsPerMinute = 0,   // invalid: must be > 0
                BurstCapacity = -1,      // invalid: must be >= 0
                CleanupIntervalSeconds = 0 // invalid: must be > 0
            };
        }

        private static TokenBucket CreateTokenBucket()
        {
            // TokenBucket has no public validation state; we can create an uninitialized instance.
            return (TokenBucket)FormatterServices.GetUninitializedObject(typeof(TokenBucket));
        }

        private static RateLimitingMiddleware CreateMiddleware(RateLimitingOptions options)
        {
            // RateLimitingMiddleware is likely sealed and has no parameterless ctor.
            // Use FormatterServices to bypass the constructor and inject the private _options field.
            var middleware = (RateLimitingMiddleware)FormatterServices.GetUninitializedObject(typeof(RateLimitingMiddleware));

            // Find the private field named "_options" and set it.
            var field = typeof(RateLimitingMiddleware).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("Unable to locate private field '_options' on RateLimitingMiddleware.");

            field.SetValue(middleware, options);
            return middleware;
        }

        [Fact]
        public void Validate_RateLimitingOptions_WithValidValues_ReturnsEmpty()
        {
            var options = CreateValidOptions();

            IReadOnlyList<string> problems = options.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void Validate_RateLimitingOptions_WithInvalidValues_ReturnsProblems()
        {
            var options = CreateInvalidOptions();

            IReadOnlyList<string> problems = options.Validate();

            Assert.Collection(problems,
                p => Assert.Contains(nameof(RateLimitingOptions.RequestsPerMinute), p),
                p => Assert.Contains(nameof(RateLimitingOptions.BurstCapacity), p),
                p => Assert.Contains(nameof(RateLimitingOptions.CleanupIntervalSeconds), p));
        }

        [Fact]
        public void IsValid_RateLimitingOptions_Valid_ReturnsTrue()
        {
            var options = CreateValidOptions();

            Assert.True(options.IsValid());
        }

        [Fact]
        public void IsValid_RateLimitingOptions_Invalid_ReturnsFalse()
        {
            var options = CreateInvalidOptions();

            Assert.False(options.IsValid());
        }

        [Fact]
        public void EnsureValid_RateLimitingOptions_Valid_DoesNotThrow()
        {
            var options = CreateValidOptions();

            var exception = Record.Exception(() => options.EnsureValid());

            Assert.Null(exception);
        }

        [Fact]
        public void EnsureValid_RateLimitingOptions_Invalid_ThrowsArgumentException()
        {
            var options = CreateInvalidOptions();

            var ex = Assert.Throws<ArgumentException>(() => options.EnsureValid());

            Assert.Contains(nameof(RateLimitingOptions.RequestsPerMinute), ex.Message);
            Assert.Contains(nameof(RateLimitingOptions.BurstCapacity), ex.Message);
            Assert.Contains(nameof(RateLimitingOptions.CleanupIntervalSeconds), ex.Message);
        }

        [Fact]
        public void Validate_TokenBucket_ReturnsEmpty()
        {
            var bucket = CreateTokenBucket();

            IReadOnlyList<string> problems = bucket.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void IsValid_TokenBucket_ReturnsTrue()
        {
            var bucket = CreateTokenBucket();

            Assert.True(bucket.IsValid());
        }

        [Fact]
        public void EnsureValid_TokenBucket_DoesNotThrow()
        {
            var bucket = CreateTokenBucket();

            var exception = Record.Exception(() => bucket.EnsureValid());

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_RateLimitingMiddleware_Null_ThrowsArgumentNullException()
        {
            RateLimitingMiddleware? middleware = null;

            Assert.Throws<ArgumentNullException>(() => middleware!.Validate());
        }

        [Fact]
        public void EnsureValid_RateLimitingMiddleware_Null_ThrowsArgumentNullException()
        {
            RateLimitingMiddleware? middleware = null;

            Assert.Throws<ArgumentNullException>(() => middleware!.EnsureValid());
        }

        [Fact]
        public void EnsureValid_RateLimitingMiddleware_WithInvalidOptions_ThrowsArgumentException()
        {
            var invalidOptions = CreateInvalidOptions();
            var middleware = CreateMiddleware(invalidOptions);

            var ex = Assert.Throws<ArgumentException>(() => middleware.EnsureValid());

            // The exception should contain the problems from the options validation.
            Assert.Contains(nameof(RateLimitingOptions.RequestsPerMinute), ex.Message);
            Assert.Contains(nameof(RateLimitingOptions.BurstCapacity), ex.Message);
            Assert.Contains(nameof(RateLimitingOptions.CleanupIntervalSeconds), ex.Message);
        }

        [Fact]
        public void Validate_RateLimitingMiddleware_WithValidOptions_ReturnsEmpty()
        {
            var validOptions = CreateValidOptions();
            var middleware = CreateMiddleware(validOptions);

            IReadOnlyList<string> problems = middleware.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void IsValid_RateLimitingMiddleware_WithValidOptions_ReturnsTrue()
        {
            var validOptions = CreateValidOptions();
            var middleware = CreateMiddleware(validOptions);

            Assert.True(middleware.IsValid());
        }

        [Fact]
        public void IsValid_RateLimitingMiddleware_WithInvalidOptions_ReturnsFalse()
        {
            var invalidOptions = CreateInvalidOptions();
            var middleware = CreateMiddleware(invalidOptions);

            Assert.False(middleware.IsValid());
        }
    }
}
