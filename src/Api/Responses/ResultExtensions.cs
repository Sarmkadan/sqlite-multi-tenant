using System;
using System.Collections.Generic;
using System.Linq;

namespace SqliteMultiTenant.Api.Responses
{
    public static class ResultExtensions
    {
        public static Result<T> AddMetadata<T>(this Result<T> result, ResultMetadata metadata)
        {
            result.Metadata = metadata;
            return result;
        }

        public static Result<T> AddError<T>(this Result<T> result, string error)
        {
            result.Errors.Add(error);
            return result;
        }

        public static Result<T> AddData<T>(this Result<T> result, T data)
        {
            result.Data = data;
            return result;
        }
    }
}
