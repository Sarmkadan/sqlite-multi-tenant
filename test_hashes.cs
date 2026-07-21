using System;
using SqliteMultiTenant.Utilities;

Console.WriteLine("MD5 Hashes:");
Console.WriteLine($"\"Hello World\": {StringUtilities.ComputeMd5Hash("Hello World")}");
Console.WriteLine($"\"hello\": {StringUtilities.ComputeMd5Hash("hello")}");
Console.WriteLine($"\"test\": {StringUtilities.ComputeMd5Hash("test")}");
Console.WriteLine($"\"1234567890\": {StringUtilities.ComputeMd5Hash("1234567890")}");

Console.WriteLine("\nSHA256 Hashes:");
Console.WriteLine($"\"1234567890\": {StringUtilities.ComputeSha256Hash("1234567890")}");
Console.WriteLine($"\"hello\": {StringUtilities.ComputeSha256Hash("hello")}");
Console.WriteLine($"\"test\": {StringUtilities.ComputeSha256Hash("test")}");

Console.WriteLine("\nGUID Tests:");
Console.WriteLine($"\"550e8400-e29b-41d4-a716-446655440000\": {StringUtilities.IsValidGuid("550e8400-e29b-41d4-a716-446655440000")}");
Console.WriteLine($"\"550e8400e29b41d4a716446655440000\": {StringUtilities.IsValidGuid("550e8400e29b41d4a716446655440000")}");

Console.WriteLine("\nSnake Case Tests:");
Console.WriteLine($"\"HTTPSConnection\": {StringUtilities.ToSnakeCase("HTTPSConnection")}");
