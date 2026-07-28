#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SqliteMultiTenant.Middleware;
using SqliteMultiTenant.Api; // assuming Result<T> lives in this namespace
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class ErrorHandlingMiddlewareValidationTests
    {
        // -----------------------------------------------------------------
        // Helper to create a dummy ErrorHandlingMiddleware instance.
        // The real middleware likely has a constructor that requires a logger.
        // We bypass the constructor using FormatterServices, as the validation
        // only checks for null.
        // -----------------------------------------------------------------
        private static ErrorHandlingMiddleware CreateMiddleware()
        {
            return (ErrorHandlingMiddleware)FormatterServices.GetUninitializedObject(
                typeof(ErrorHandlingMiddleware));
        }

        // -----------------------------------------------------------------
        // Helper to create a Result<T> instance without invoking its ctor.
        // The validation logic only inspects the public properties.
        // -----------------------------------------------------------------
        private static Result<T> CreateResult<T>(bool isSuccess, string? errorMessage, T value)
        {
            var result = (Result<T>)FormatterServices.GetUninitializedObject(typeof(Result<T>));

            var isSuccessProp = typeof(Result<T>).GetProperty("IsSuccess");
            var errorMessageProp = typeof(Result<T>).GetProperty("ErrorMessage");
            var valueProp = typeof(Result<T>).GetProperty("Value");

            isSuccessProp?.SetValue(result, isSuccess);
            errorMessageProp?.SetValue(result, errorMessage);
            valueProp?.SetValue(result, value);

            return result;
        }

        // -------------------- ErrorHandlingMiddleware --------------------

        [Fact]
        public void Validate_ErrorHandlingMiddleware_NonNull_ReturnsEmpty()
        {
            var middleware = CreateMiddleware();

            IReadOnlyList<string> problems = middleware.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void IsValid_ErrorHandlingMiddleware_NonNull_ReturnsTrue()
        {
            var middleware = CreateMiddleware();

            Assert.True(middleware.IsValid());
        }

        [Fact]
        public void EnsureValid_ErrorHandlingMiddleware_NonNull_DoesNotThrow()
        {
            var middleware = CreateMiddleware();

            var ex = Record.Exception(() => middleware.EnsureValid());

            Assert.Null(ex);
        }

        [Fact]
        public void Validate_ErrorHandlingMiddleware_Null_ThrowsArgumentNullException()
        {
            ErrorHandlingMiddleware? middleware = null;

            Assert.Throws<ArgumentNullException>(() => middleware!.Validate());
        }

        [Fact]
        public void EnsureValid_ErrorHandlingMiddleware_Null_ThrowsArgumentNullException()
        {
            ErrorHandlingMiddleware? middleware = null;

            Assert.Throws<ArgumentNullException>(() => middleware!.EnsureValid());
        }

        // --------------------------- Result<T> ---------------------------

        [Fact]
        public void Validate_Result_SuccessValid_ReturnsEmpty()
        {
            var result = CreateResult( isSuccess: true, errorMessage: null, value: 42 );

            IReadOnlyList<string> problems = result.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void Validate_Result_FailureValid_ReturnsEmpty()
        {
            var result = CreateResult( isSuccess: false, errorMessage: "boom", value: default(int) );

            IReadOnlyList<string> problems = result.Validate();

            Assert.Empty(problems);
        }

        [Fact]
        public void Validate_Result_SuccessWithErrorMessage_ReturnsProblem()
        {
            var result = CreateResult( isSuccess: true, errorMessage: "should be null", value: 1 );

            IReadOnlyList<string> problems = result.Validate();

            Assert.Single(problems);
            Assert.Contains("ErrorMessage", problems[0]);
        }

        [Fact]
        public void Validate_Result_FailureWithoutErrorMessage_ReturnsProblem()
        {
            var result = CreateResult( isSuccess: false, errorMessage: null, value: default(int) );

            IReadOnlyList<string> problems = result.Validate();

            Assert.Single(problems);
            Assert.Contains("ErrorMessage", problems[0]);
        }

        [Fact]
        public void Validate_Result_SuccessWithDefaultValue_ReturnsProblem()
        {
            var result = CreateResult( isSuccess: true, errorMessage: null, value: default(int) );

            IReadOnlyList<string> problems = result.Validate();

            Assert.Single(problems);
            Assert.Contains("Value", problems[0]);
        }

        [Fact]
        public void IsValid_Result_ValidSuccess_ReturnsTrue()
        {
            var result = CreateResult( isSuccess: true, errorMessage: null, value: 99 );

            Assert.True(result.IsValid());
        }

        [Fact]
        public void IsValid_Result_InvalidFailure_ReturnsFalse()
        {
            var result = CreateResult( isSuccess: false, errorMessage: null, value: default(int) );

            Assert.False(result.IsValid());
        }

        [Fact]
        public void EnsureValid_Result_ValidDoesNotThrow()
        {
            var result = CreateResult( isSuccess: true, errorMessage: null, value: 123 );

            var ex = Record.Exception(() => result.EnsureValid());

            Assert.Null(ex);
        }

        [Fact]
        public void EnsureValid_Result_InvalidThrowsArgumentException()
        {
            var result = CreateResult( isSuccess: true, errorMessage: "oops", value: 0 );

            var ex = Assert.Throws<ArgumentException>(() => result.EnsureValid());

            Assert.Contains("ErrorMessage", ex.Message);
            Assert.Contains("Value", ex.Message);
        }

        [Fact]
        public void Validate_Result_Null_ThrowsArgumentNullException()
        {
            Result<int>? result = null;

            Assert.Throws<ArgumentNullException>(() => result!.Validate());
        }

        [Fact]
        public void EnsureValid_Result_Null_ThrowsArgumentNullException()
        {
            Result<int>? result = null;

            Assert.Throws<ArgumentNullException>(() => result!.EnsureValid());
        }
    }
}
