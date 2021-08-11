#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net;
using SqliteMultiTenant.Api.Responses;

namespace SqliteMultiTenant.Api
{
    // Fluent builder for constructing consistent API responses
    // Ensures standardized response structure across all endpoints
    public sealed class ApiResponseBuilder<T> {
        private T _data;
        private HttpStatusCode _statusCode;
        private string _message;
        private List<ApiError> _errors;
        private Dictionary<string, object> _metadata;
        private bool _success;

        public ApiResponseBuilder()
        {
            _errors = new List<ApiError>();
            _metadata = new Dictionary<string, object>();
            _statusCode = HttpStatusCode.OK;
        }

        // Sets the response data
        public ApiResponseBuilder<T> WithData(T data)
        {
            _data = data;
            return this;
        }

        // Sets the HTTP status code
        public ApiResponseBuilder<T> WithStatusCode(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }

        // Sets the response message
        public ApiResponseBuilder<T> WithMessage(string message)
        {
            _message = message;
            return this;
        }

        // Adds an error
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
        public ApiResponseBuilder<T> AddErrors(IEnumerable<ApiError> errors)
        {
            _errors.AddRange(errors);
            return this;
        }

        // Adds metadata
        public ApiResponseBuilder<T> AddMetadata(string key, object value)
        {
            _metadata[key] = value;
            return this;
        }

        // Marks response as success
        public ApiResponseBuilder<T> Success()
        {
            _success = true;
            if (_statusCode == HttpStatusCode.OK)
                _statusCode = HttpStatusCode.OK;

            return this;
        }

        // Marks response as failure
        public ApiResponseBuilder<T> Failure()
        {
            _success = false;
            if (_statusCode == HttpStatusCode.OK)
                _statusCode = HttpStatusCode.BadRequest;

            return this;
        }

        // Builds a success response (201 Created)
        public ApiResponseBuilder<T> Created()
        {
            _success = true;
            _statusCode = HttpStatusCode.Created;
            _message = _message ?? "Resource created successfully";

            return this;
        }

        // Builds an accepted response (202 Accepted)
        public ApiResponseBuilder<T> Accepted()
        {
            _success = true;
            _statusCode = HttpStatusCode.Accepted;
            _message = _message ?? "Request accepted for processing";

            return this;
        }

        // Builds a not found response (404 Not Found)
        public ApiResponseBuilder<T> NotFound(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.NotFound;
            _message = message ?? "Resource not found";
            _errors.Add(new ApiError { Message = _message, Code = "NOT_FOUND" });

            return this;
        }

        // Builds a conflict response (409 Conflict)
        public ApiResponseBuilder<T> Conflict(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.Conflict;
            _message = message ?? "Request conflicts with existing state";
            _errors.Add(new ApiError { Message = _message, Code = "CONFLICT" });

            return this;
        }

        // Builds an unauthorized response (401 Unauthorized)
        public ApiResponseBuilder<T> Unauthorized(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.Unauthorized;
            _message = message ?? "Authentication required";
            _errors.Add(new ApiError { Message = _message, Code = "UNAUTHORIZED" });

            return this;
        }

        // Builds a forbidden response (403 Forbidden)
        public ApiResponseBuilder<T> Forbidden(string message = null)
        {
            _success = false;
            _statusCode = HttpStatusCode.Forbidden;
            _message = message ?? "Access denied";
            _errors.Add(new ApiError { Message = _message, Code = "FORBIDDEN" });

            return this;
        }

        // Builds a validation error response (400 Bad Request)
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
                Errors = _errors.Count > 0 ? _errors : null,
                StatusCode = (int)_statusCode,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    // Builder for exception responses
    public sealed class ExceptionResponseBuilder {
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

    public sealed class ApiError {
        public string Message { get; set; }
        public string Code { get; set; }
        public string Field { get; set; }
        public object Detail { get; set; }
    }
}
