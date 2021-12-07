#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SqliteMultiTenant.Tests
{
    /// <summary>
    /// Helpers for asserting calls made through <see cref="ILogger"/> extension methods
    /// (LogInformation/LogWarning/LogDebug/LogError, etc.) on an NSubstitute mock.
    /// The Microsoft.Extensions.Logging "FormattedLogValues" state object used internally
    /// by those extension methods does not implement value equality, so a plain
    /// <c>Received(1).LogInformation("template", args)</c> assertion never matches
    /// (each call produces a distinct instance). These helpers instead assert against
    /// the underlying <see cref="ILogger.Log"/> call using the rendered message text.
    /// </summary>
    internal static class LoggerAssertionExtensions
    {
        public static void AssertLogged<T>(this ILogger<T> logger, LogLevel level, int times,
            string messageTemplate, params object?[] args)
        {
            var expected = RenderTemplate(messageTemplate, args);
            logger.Received(times).Log(
                level,
                Arg.Any<EventId>(),
                Arg.Is<object>(state => state != null && state.ToString() == expected),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Asserts that a log call at the given level was made whose rendered message
        /// contains <paramref name="expectedSubstring"/>.
        /// </summary>
        public static void AssertLoggedContains<T>(this ILogger<T> logger, LogLevel level, int times,
            string expectedSubstring)
        {
            logger.Received(times).Log(
                level,
                Arg.Any<EventId>(),
                Arg.Is<object>(state => state != null && state.ToString()!.Contains(expectedSubstring)),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Asserts that a log call at the given level was made, without checking the
        /// rendered message text.
        /// </summary>
        public static void AssertLoggedAny<T>(this ILogger<T> logger, LogLevel level, int times = 1)
        {
            logger.Received(times).Log(
                level,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        public static void AssertLoggedWithException<T>(this ILogger<T> logger, LogLevel level, int times,
            Type exceptionType, string messageTemplate, params object?[] args)
        {
            var expected = RenderTemplate(messageTemplate, args);
            logger.Received(times).Log(
                level,
                Arg.Any<EventId>(),
                Arg.Is<object>(state => state != null && state.ToString() == expected),
                Arg.Is<Exception>(ex => ex != null && exceptionType.IsInstanceOfType(ex)),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Renders a structured logging message template the same way
        /// Microsoft.Extensions.Logging's FormattedLogValues does: each
        /// "{Placeholder}" token is substituted, in order, with the next
        /// positional argument.
        /// </summary>
        private static string RenderTemplate(string messageTemplate, object?[] args)
        {
            var index = 0;
            return Regex.Replace(messageTemplate, "\\{[^{}]+\\}", _ =>
                index < args.Length ? Convert.ToString(args[index++]) ?? string.Empty : string.Empty);
        }
    }
}
