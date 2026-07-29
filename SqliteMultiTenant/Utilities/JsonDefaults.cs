using System;
using System.Text.Json;
using System.Collections.Generic;

namespace SqliteMultiTenant.Utilities
{
    public static class JsonDefaults
    {
        public static JsonSerializerOptions Option { get; } = new JsonSerializerOptions
        {
            PropertyNameCase = PropertyNameCase.CamelCase,
            WriteIndented = true
        };
    }
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNameCase = PropertyNameCase.CamelCase,
        WriteIndented = true
    };
}