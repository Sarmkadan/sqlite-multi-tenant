#nullable enable

using System;
using System.Text.Json;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Constants;

Console.WriteLine("Testing MigrationJsonExtensions...\n");

// Create a test migration
var migration = new Migration
{
    MigrationId = "test-migration-001",
    DatabaseId = "db-001",
    Version = "1.0.0",
    Name = "InitialSetup",
    Description = "Initial database setup migration",
    UpScript = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT);",
    DownScript = "DROP TABLE TestTable;",
    Status = MigrationStatus.Completed,
    CreatedAt = DateTime.UtcNow,
    ExecutedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow.AddMinutes(-3),
    ExecutedBy = "test-user",
    ExecutionTimeMs = 150,
    ExecutionOrder = 1,
    IsRollbackable = true
};

Console.WriteLine("1. Testing ToJson with compact format...");
try
{
    string jsonCompact = migration.ToJson();
    Console.WriteLine("✓ ToJson() succeeded");
    Console.WriteLine($"JSON length: {jsonCompact.Length} characters");
    Console.WriteLine("First 100 chars:");
    Console.WriteLine(jsonCompact[..Math.Min(100, jsonCompact.Length)]);
}
catch (Exception ex)
{
    Console.WriteLine($"✗ ToJson() failed: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n2. Testing ToJson with indented format...");
try
{
    string jsonIndented = migration.ToJson(indented: true);
    Console.WriteLine("✓ ToJson(indented: true) succeeded");
    Console.WriteLine("First 150 chars:");
    Console.WriteLine(jsonIndented[..Math.Min(150, jsonIndented.Length)]);
}
catch (Exception ex)
{
    Console.WriteLine($"✗ ToJson(indented: true) failed: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n3. Testing FromJson...");
try
{
    string json = migration.ToJson();
    Migration? deserialized = MigrationJsonExtensions.FromJson(json);
    if (deserialized == null)
    {
        Console.WriteLine("✗ FromJson returned null");
        Environment.Exit(1);
    }
    Console.WriteLine("✓ FromJson succeeded");
    Console.WriteLine($"Deserialized MigrationId: {deserialized.MigrationId}");
    Console.WriteLine($"Deserialized Version: {deserialized.Version}");
    Console.WriteLine($"Deserialized Status: {deserialized.Status}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ FromJson failed: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n4. Testing TryFromJson with valid JSON...");
try
{
    string json = migration.ToJson();
    bool success = MigrationJsonExtensions.TryFromJson(json, out var result);
    if (!success || result == null)
    {
        Console.WriteLine("✗ TryFromJson returned false or null");
        Environment.Exit(1);
    }
    Console.WriteLine("✓ TryFromJson succeeded with valid JSON");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ TryFromJson failed: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n5. Testing TryFromJson with invalid JSON...");
try
{
    bool success = MigrationJsonExtensions.TryFromJson("invalid json {{{", out var result);
    if (success)
    {
        Console.WriteLine("✗ TryFromJson should have returned false for invalid JSON");
        Environment.Exit(1);
    }
    Console.WriteLine("✓ TryFromJson correctly returned false for invalid JSON");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ TryFromJson threw exception on invalid JSON: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n6. Testing TryFromJson with empty/whitespace JSON...");
try
{
    bool success1 = MigrationJsonExtensions.TryFromJson("", out var result1);
    bool success2 = MigrationJsonExtensions.TryFromJson("   ", out var result2);
    bool success3 = MigrationJsonExtensions.TryFromJson(null, out var result3);

    if (success1 || success2 || success3)
    {
        Console.WriteLine("✗ TryFromJson should have returned false for empty/whitespace/null JSON");
        Environment.Exit(1);
    }
    Console.WriteLine("✓ TryFromJson correctly returned false for empty/whitespace/null JSON");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ TryFromJson threw exception on empty JSON: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n7. Testing FromJson with empty/whitespace JSON...");
try
{
    Migration? result1 = MigrationJsonExtensions.FromJson("");
    Migration? result2 = MigrationJsonExtensions.FromJson("   ");
    Migration? result3 = MigrationJsonExtensions.FromJson(null);

    if (result1 != null || result2 != null || result3 != null)
    {
        Console.WriteLine("✗ FromJson should have returned null for empty/whitespace/null JSON");
        Environment.Exit(1);
    }
    Console.WriteLine("✓ FromJson correctly returned null for empty/whitespace/null JSON");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ FromJson threw exception on empty JSON: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n8. Testing camelCase property naming...");
try
{
    string json = migration.ToJson();
    if (!json.Contains("migrationId") || !json.Contains("databaseId") || !json.Contains("upScript"))
    {
        Console.WriteLine("✗ JSON does not contain expected camelCase properties");
        Console.WriteLine("Expected properties: migrationId, databaseId, upScript");
        Environment.Exit(1);
    }
    Console.WriteLine("✓ JSON correctly uses camelCase property naming");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ camelCase test failed: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n✅ All tests passed! MigrationJsonExtensions is working correctly.");
