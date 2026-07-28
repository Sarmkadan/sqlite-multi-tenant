using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Middleware;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Tests for <see cref="PerformanceMiddleware"/>.
    /// Covers happy path, slow‑request logging, null next delegate handling,
    /// and verification of the populated <see cref="RequestMetrics"/>.
    /// </summary>
    public class PerformanceMiddlewareTests
    {
        private sealed class TestLogger<T> : ILogger<T>
        {
            public LogLevel? LastLogLevel { get; private set; }
            public string? LastMessage { get; private set; }

            public IDisposable? BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LastLogLevel = logLevel;
                LastMessage = formatter(state, exception);
            }
        }

        [Fact]
        public async Task InvokeAsync_HappyPath_StoresMetricsAndHeaders()
        {
            // Arrange
            var logger = new TestLogger<PerformanceMiddleware>();
            RequestDelegate next = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            };
            var middleware = new PerformanceMiddleware(next, logger);
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert: metrics stored in HttpContext.Items
            Assert.True(context.Items.TryGetValue("RequestMetrics", out var metricsObj));
            var metrics = Assert.IsType<RequestMetrics>(metricsObj);
            Assert.Equal(context.Request.Method, metrics.Method);
            Assert.Equal(context.Request.Path.ToString(), metrics.Path);
            Assert.Equal(context.Response.StatusCode, metrics.StatusCode);
            Assert.True(metrics.ElapsedMs > 0);
            Assert.True(metrics.MemoryUsedKb >= 0);
            Assert.True((DateTime.UtcNow - metrics.Timestamp).TotalSeconds < 5);

            // Assert: response headers added
            Assert.True(context.Response.Headers.ContainsKey("X-Response-Time-Ms"));
            Assert.True(context.Response.Headers.ContainsKey("X-Memory-Used-Kb"));
            Assert.True(long.TryParse(context.Response.Headers["X-Response-Time-Ms"], out var _));
            Assert.True(long.TryParse(context.Response.Headers["X-Memory-Used-Kb"], out var _));

            // Assert: logger recorded an Information entry (fast request)
            Assert.Equal(LogLevel.Information, logger.LastLogLevel);
            Assert.NotNull(logger.LastMessage);
            Assert.Contains("completed in", logger.LastMessage);
        }

        [Fact]
        public async Task InvokeAsync_SlowRequest_LogsWarning()
        {
            // Arrange: set threshold to 0 so any elapsed time triggers warning
            var logger = new TestLogger<PerformanceMiddleware>();
            RequestDelegate next = async ctx =>
            {
                // Simulate some work
                await Task.Delay(10);
                ctx.Response.StatusCode = StatusCodes.Status200OK;
            };
            var middleware = new PerformanceMiddleware(next, logger, slowRequestThresholdMs: 0);
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert: logger recorded a Warning entry
            Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
            Assert.NotNull(logger.LastMessage);
            Assert.Contains("Slow request detected", logger.LastMessage);
        }

        [Fact]
        public async Task InvokeAsync_NullNextDelegate_ThrowsNullReferenceException()
        {
            // Arrange
            var logger = new TestLogger<PerformanceMiddleware>();
            var middleware = new PerformanceMiddleware(null!, logger);
            var context = new DefaultHttpContext();

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => middleware.InvokeAsync(context));
        }

        [Fact]
        public async Task InvokeAsync_ResponseBodyIsPreserved()
        {
            // Arrange
            var logger = new TestLogger<PerformanceMiddleware>();
            const string responseContent = "hello world";
            RequestDelegate next = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                await ctx.Response.WriteAsync(responseContent);
            };
            var middleware = new PerformanceMiddleware(next, logger);
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context);
            context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            using var reader = new System.IO.StreamReader(context.Response.Body);
            var body = await reader.ReadToEndAsync();

            // Assert: original response body content is unchanged
            Assert.Equal(responseContent, body);
        }
    }
}
