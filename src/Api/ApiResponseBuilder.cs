#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SqliteMultiTenant.Api.Responses;

namespace SqliteMultiTenant.Api
{
    /// <summary>
    /// Fluent builder for constructing consistent API responses with standardized structure.
    /// Provides a fluent interface to build well-formed API responses with proper status codes,
    /// error handling, and metadata across all endpoints.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the response.</typeparam>
    // Fluent builder for constructing consistent API responses
    // Ensures standardized response structure across all endpoints
    public sealed class ApiResponseBuilder<T>
    {
        private T _data;
        private HttpStatusCode _statusCode;
        private string _message;
        private List<ApiError> _errors;
        private Dictionary<string, object> _metadata;
        private bool _success;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponseBuilder{T}"/> class with default values.
        /// Sets up empty collections for errors and metadata, and initializes status code to OK.
        /// </summary>
        public ApiResponseBuilder()
        {
            _errors = new List<ApiError>();
            _metadata = new Dictionary<string, object>();
            _statusCode = HttpStatusCode.OK;
        }

        // Sets the response data
        /// <summary>
        /// Sets the data to be included in the API response.
        /// </summary>
        /// <param name="data">The data payload to include in the response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> WithData(T data)
        {
            _data = data;
            return this;
        }

        // Sets the HTTP status code
        /// <summary>
        /// Sets the HTTP status code for the API response.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> WithStatusCode(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }

        // Sets the response message
        /// <summary>
        /// Sets the message to be included in the API response.
        /// </summary>
        /// <param name="message">The message text to include in the response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> WithMessage(string message)
        {
            _message = message;
            return this;
        }

        // Adds an error
        /// <summary>
        /// Adds a single error to the API response.
        /// </summary>
        /// <param name="message">The error message describing what went wrong.</param>
        /// <param name="code">Optional error code identifying the error type.</param>
        /// <param name="field">Optional field name associated with the error.</param>
        /// <param name="detail">Optional additional details about the error.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> AddError(string message, string code = null,
            string field = null, object detail = null)
        {
            _errors.Add(new ApiError
            {
                Message = message,
                Code = code,
                Field = field,
                Detail = detail
            });

            return this;
        }

        // Adds errors from a collection
        /// <summary>
        /// Adds multiple errors to the API response from an enumerable collection.
        /// </summary>
        /// <param name="errors">Collection of errors to add to the response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> AddErrors(IEnumerable<ApiError> errors)
        {
            _errors.AddRange(errors);
            return this;
        }

        // Adds metadata
        /// <summary>
        /// Adds metadata key-value pair to the API response.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> AddMetadata(string key, object value)
        {
            _metadata[key] = value;
            return this;
        }

        // Marks response as success
        /// <summary>
        /// Marks the response as successful.
        /// </summary>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Success()
        {
            _success = true;
            if (_statusCode == HttpStatusCode.OK)
                _statusCode = HttpStatusCode.OK;

            return this;
        }

        // Marks response as failure
        /// <summary>
        /// Marks the response as failed.
        /// </summary>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Failure()
        {
            _success = false;
            if (_statusCode == HttpStatusCode.OK)
                _statusCode = HttpStatusCode.BadRequest;

            return this;
        }

        // Builds a success response (201 Created)
        /// <summary>
        /// Marks the response as successful with HTTP 201 Created status.
        /// Sets default success message if not already set.
        /// </summary>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Created()
        {
            _success = true;
            _statusCode = HttpStatusCode.Created;
            _message = _message ?? "Resource created successfully";

            return this;
        }

        // Builds an accepted response (202 Accepted)
        /// <summary>
        /// Marks the response as successful with HTTP 202 Accepted status.
        /// Sets default success message if not already set.
        /// </summary>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Accepted()
        {
            _success = true;
            _statusCode = HttpStatusCode.Accepted;
            _message = _message ?? "Request accepted for processing";

            return this;
        }

        // Builds a not found response (404 Not Found)
        /// <summary>
        /// Marks the response as failed with HTTP 404 Not Found status.
        /// Adds a NOT_FOUND error to the response.
        /// </summary>
        /// <param name="message">Optional custom message for the not found response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> NotFound(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.NotFound;
            _message = message ?? "Resource not found";
            _errors.Add(new ApiError { Message = _message, Code = "NOT_FOUND" });

            return this;
        }

        // Builds a conflict response (409 Conflict)
        /// <summary>
        /// Marks the response as failed with HTTP 409 Conflict status.
        /// Adds a CONFLICT error to the response.
        /// </summary>
        /// <param name="message">Optional custom message for the conflict response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Conflict(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.Conflict;
            _message = message ?? "Request conflicts with existing state";
            _errors.Add(new ApiError { Message = _message, Code = "CONFLICT" });

            return this;
        }

        // Builds an unauthorized response (401 Unauthorized)
        /// <summary>
        /// Marks the response as failed with HTTP 401 Unauthorized status.
        /// Adds a UNAUTHORIZED error to the response.
        /// </summary>
        /// <param name="message">Optional custom message for the unauthorized response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Unauthorized(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.Unauthorized;
            _message = message ?? "Authentication required";
            _errors.Add(new ApiError { Message = _message, Code = "UNAUTHORIZED" });

            return this;
        }

        // Builds a forbidden response (403 Forbidden)
        /// <summary>
        /// Marks the response as failed with HTTP 403 Forbidden status.
        /// Adds a FORBIDDEN error to the response.
        /// </summary>
        /// <param name="message">Optional custom message for the forbidden response.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> Forbidden(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.Forbidden;
            _message = message ?? "Access denied";
            _errors.Add(new ApiError { Message = _message, Code = "FORBIDDEN" });

            return this;
        }

        // Builds a validation error response (400 Bad Request)
        /// <summary>
        /// Marks the response as failed with HTTP 400 Bad Request status for validation errors.
        /// Adds VALIDATION_ERROR entries to the response.
        /// </summary>
        /// <param name="fieldErrors">Optional dictionary of field-specific validation errors.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> ValidationError(Dictionary<string, List<string>> fieldErrors = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.BadRequest;
            _message = _message ?? "Validation failed";

            if (fieldErrors is not null)
            {
                foreach (var field in fieldErrors)
                {
                    foreach (var error in field.Value)
                    {
                        _errors.Add(new ApiError
                        {
                            Message = error,
                            Code = "VALIDATION_ERROR",
                            Field = field.Key
                        });
                    }
                }
            }

            return this;
        }

        // Builds a server error response (500 Internal Server Error)
        /// <summary>
        /// Marks the response as failed with HTTP 500 Internal Server Error status.
        /// Adds an INTERNAL_ERROR entry to the response.
        /// </summary>
        /// <param name="message">Optional custom message for the server error response.</param>
        /// <param name="exception">Optional exception to include details from.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> ServerError(string message = null, Exception exception = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.InternalServerError;
            _message = message ?? "An internal error occurred";

            _errors.Add(new ApiError
            {
                Message = _message,
                Code = "INTERNAL_ERROR",
                Detail = exception?.Message
            });

            return this;
        }

        // Builds a too many requests response (429 Too Many Requests)
        /// <summary>
        /// Marks the response as failed with HTTP 429 Too Many Requests status.
        /// Optionally adds retry-after metadata.
        /// </summary>
        /// <param name="message">Optional custom message for the rate limit response.</param>
        /// <param name="retryAfter">Optional value indicating when to retry the request.</param>
        /// <returns>The current builder instance for fluent chaining.</returns>
        public ApiResponseBuilder<T> TooManyRequests(string message = null, object retryAfter = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.TooManyRequests;
            _message = message ?? "Rate limit exceeded";

            if (retryAfter is not null)
            {
                _metadata["retryAfter"] = retryAfter;
            }

            return this;
        }

        // Builds the final response
        /// <summary>
        /// Builds and returns the final <see cref="ApiResponse{T}"/> object.
        /// Automatically determines success status based on status code and errors.
        /// </summary>
        /// <returns>A configured <see cref="ApiResponse{T}"/> instance ready for use.</returns>
        public ApiResponse<T> Build()
        {
            // Auto-determine success if not explicitly set
            if (_statusCode < HttpStatusCode.BadRequest && _errors.Count == 0)
            {
                _success = true;
            }
            else if (_statusCode >= HttpStatusCode.BadRequest)
            {
                _success = false;
            }

            return new ApiResponse<T>
            {
                IsSuccess = _success,
                Data = _data,
                Message = _message,
                Errors = _errors.Count > 0
                    ? _errors
                        .Select((e, i) => new { Key = e.Code ?? e.Field ?? i.ToString(), Value = e.Message })
                        .GroupBy(kv => kv.Key)
                        .ToDictionary(g => g.Key, g => g.First().Value)
                    : null,
                StatusCode = (int)_statusCode,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    // Builder for exception responses
    /// <summary>
    /// Static builder class for creating API responses from exceptions.
    /// Converts common exception types to appropriate error responses.
    /// </summary>
    public sealed class ExceptionResponseBuilder
    {
        /// <summary>
        /// Creates an API response from an exception.
        /// Maps specific exception types to appropriate error responses:
        /// ArgumentNullException → ValidationError, ArgumentException → ValidationError,
        /// InvalidOperationException → Conflict, UnauthorizedAccessException → Forbidden,
        /// TimeoutException → ServerError, others → ServerError with exception details.
        /// </summary>
        /// <param name="ex">The exception to convert to an API response.</param>
        /// <param name="userMessage">Optional custom message to override default error messages.</param>
        /// <returns>An <see cref="ApiResponseBuilder{object}"/> configured with the exception details.</returns>
        public static ApiResponseBuilder<object> FromException(Exception ex, string userMessage = null)
        {
            var builder = new ApiResponseBuilder<object>();

            switch (ex)
            {
                case ArgumentNullException argEx:
                    builder.ValidationError(new Dictionary<string, List<string>>
                    {
                        { argEx.ParamName ?? "parameter", new List<string> { "Value cannot be null" } }
                    });
                    break;

                case ArgumentException argEx:
                    builder.ValidationError(new Dictionary<string, List<string>>
                    {
                        { argEx.ParamName ?? "parameter", new List<string> { argEx.Message } }
                    });
                    break;

                case InvalidOperationException _:
                    builder.Conflict(userMessage ?? ex.Message);
                    break;

                case UnauthorizedAccessException _:
                    builder.Forbidden(userMessage ?? "You do not have permission to access this resource");
                    break;

                case TimeoutException _:
                    builder.ServerError("Request timeout");
                    break;

                default:
                    builder.ServerError(userMessage ?? "An unexpected error occurred", ex);
                    break;
            }

            return builder;
        }
    }

    public sealed class ApiError
    {
        /// <summary>
        /// Gets or sets the error message describing what went wrong.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the error code identifying the error type.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the field name associated with the error, if applicable.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets additional details about the error.
        /// </summary>
        public object Detail { get; set; }
    }
}