using System;
using System.Collections.Generic;

namespace SqliteMultiTenant.Api.Responses
{
    /// <summary>
    /// Provides extension methods for <see cref="Result{T}"/> to fluently build result objects.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Adds metadata to the result.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="result">The result instance.</param>
        /// <param name="metadata">The metadata to add.</param>
        /// <returns>The result instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> or <paramref name="metadata"/> is null.</exception>
        public static Result<T> AddMetadata<T>(this Result<T> result, ResultMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(metadata);

            result.Metadata = metadata;
            return result;
        }

        /// <summary>
        /// Adds an error message to the result.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="result">The result instance.</param>
        /// <param name="error">The error message to add.</param>
        /// <returns>The result instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
        public static Result<T> AddError<T>(this Result<T> result, string error)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(error);

            result.Errors.Add(error);
            return result;
        }

        /// <summary>
        /// Adds an error message to the result with exception details.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="result">The result instance.</param>
        /// <param name="error">The error message to add.</param>
        /// <param name="exception">The exception that caused the error.</param>
        /// <returns>The result instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
        public static Result<T> AddError<T>(this Result<T> result, string error, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(error);

            result.Errors.Add(exception == null
                ? error
                : $"{error}: {exception.Message}");
            return result;
        }

        /// <summary>
        /// Sets the data for the result.
        /// </summary>
        /// <typeparam name="T">The type of data in the result.</typeparam>
        /// <param name="result">The result instance.</param>
        /// <param name="data">The data to set.</param>
        /// <returns>The result instance for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
        public static Result<T> AddData<T>(this Result<T> result, T data)
        {
            ArgumentNullException.ThrowIfNull(result);

            result.Data = data;
            return result;
        }
    }
}