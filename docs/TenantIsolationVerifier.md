# TenantIsolationVerifier

The `TenantIsolationVerifier` is a sealed utility class within the `sqlite-multi-tenant` project designed to audit and enforce data isolation boundaries between tenants in a shared SQLite database environment. It provides asynchronous mechanisms to verify the integrity of tenant separation, detect potential data leakage patterns, and validate that specific SQL queries adhere to strict tenant filtering requirements before execution.

## API

### Class: `TenantIsolationVerifier`

A sealed class responsible for executing isolation checks. It cannot be inherited.

#### Constructor
**`public TenantIsolationVerifier()`**
Initializes a new instance of the `TenantIsolationVerifier` class.

#### Methods

**`public async Task<IsolationVerificationResult> VerifyTenantIsolationAsync()`**
Performs a comprehensive audit of the current database state to ensure strict tenant isolation.
*   **Purpose**: Validates connection restrictions, audit log integrity, and general query isolation mechanisms.
*   **Parameters**: None.
*   **Return Value**: Returns an `IsolationVerificationResult` object containing the status of various isolation checks and the timestamp of verification.
*   **Exceptions**: May throw exceptions if the underlying database connection is unavailable or if the database schema is inconsistent with multi-tenant requirements.

**`public async Task<List<DataLeakageSuspicion>> DetectPotentialDataLeaksAsync()`**
Scans the database for anomalies that suggest data from one tenant might be accessible to another.
*   **Purpose**: Identifies specific instances or patterns where data leakage may have occurred or is possible.
*   **Parameters**: None.
*   **Return Value**: Returns a list of `DataLeakageSuspicion` objects. If no suspicions are found, an empty list is returned.
*   **Exceptions**: May throw exceptions during the scan process if internal database reads fail.

**`public async Task<QueryValidationResult> ValidateQueryTenantIsolationAsync(string query, string tenantId)`**
Analyzes a specific SQL query string to determine if it correctly filters data for a given tenant.
*   **Purpose**: Ensures that ad-hoc or dynamic queries include the necessary `WHERE` clauses or constraints to restrict data access to the specified `tenantId`.
*   **Parameters**:
    *   `query` (`string`): The SQL query string to validate.
    *   `tenantId` (`string`): The identifier of the tenant that should be isolated by the query.
*   **Return Value**: Returns a `QueryValidationResult` indicating whether the query contains the required tenant filter.
*   **Exceptions**: May throw `ArgumentNullException` if `query` or `tenantId` is null, or format exceptions if the query syntax is unparseable.

### Class: `IsolationVerificationResult`

A sealed class representing the outcome of a full isolation verification run.

#### Properties
*   **`public string TenantId`**: The identifier of the tenant associated with this verification context.
*   **`public bool IsIsolated`**: A global flag indicating whether the tenant is currently fully isolated.
*   **`public bool AuditLogIsolationValid`**: Indicates if the audit logs correctly segregate entries by tenant.
*   **`public bool ConnectionRestrictionValid`**: Indicates if database connection restrictions are properly enforced.
*   **`public bool QueryIsolationValid`**: Indicates if the query execution engine enforces tenant boundaries.
*   **`public DateTime VerifiedAt`**: The UTC timestamp when the verification was completed.

### Class: `DataLeakageSuspicion`

A sealed class describing a specific potential data leakage event.

#### Properties
*   **`public string Type`**: The category of the suspected leak (e.g., "Missing Filter", "Cross-Tenant Join").
*   **`public string Description`**: A human-readable explanation of the suspicion.
*   **`public string Severity`**: The severity level of the suspicion (e.g., "Low", "Medium", "Critical").

### Class: `QueryValidationResult`

A sealed class representing the result of a single query validation.

#### Properties
*   **`public string Query`**: The original query string that was validated.
*   **`public string TenantId`**: The tenant ID against which the query was validated.
*   **`public bool ContainsTenantFilter`**: A boolean indicating whether the query explicitly includes a filter for the specified tenant.

## Usage

### Example 1: Comprehensive Isolation Audit
The following example demonstrates how to instantiate the verifier and run a full isolation check. It inspects the result to ensure all subsystems (audit logs, connections, and queries) are valid before proceeding with sensitive operations.

```csharp
using System;
using System.Threading.Tasks;
using SqliteMultiTenant;

public class AuditService
{
    private readonly TenantIsolationVerifier _verifier;

    public AuditService()
    {
        _verifier = new TenantIsolationVerifier();
    }

    public async Task RunDailyAuditAsync()
    {
        var result = await _verifier.VerifyTenantIsolationAsync();

        if (!result.IsIsolated)
        {
            Console.WriteLine($"CRITICAL: Tenant {result.TenantId} isolation failed at {result.VerifiedAt}.");
            
            if (!result.AuditLogIsolationValid) Console.WriteLine(" - Audit logs are compromised.");
            if (!result.ConnectionRestrictionValid) Console.WriteLine(" - Connection restrictions are invalid.");
            if (!result.QueryIsolationValid) Console.WriteLine(" - Query isolation mechanisms failed.");
            
            // Trigger alerting mechanism
            return;
        }

        Console.WriteLine($"Tenant {result.TenantId} verification passed successfully.");
    }
}
```

### Example 2: Pre-Execution Query Validation and Leak Detection
This example shows how to validate a dynamic query before execution and subsequently scan for any existing data leakage suspicions if the validation fails or as part of a routine check.

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using SqliteMultiTenant;

public class QueryGuard
{
    private readonly TenantIsolationVerifier _verifier;

    public QueryGuard()
    {
        _verifier = new TenantIsolationVerifier();
    }

    public async Task<bool> ExecuteSafeQueryAsync(string sql, string tenantId)
    {
        // Validate the query structure
        var validationResult = await _verifier.ValidateQueryTenantIsolationAsync(sql, tenantId);

        if (!validationResult.ContainsTenantFilter)
        {
            Console.WriteLine($"Blocked query for tenant {tenantId}: Missing tenant filter.");
            // Optionally scan for leaks if a bad query was attempted
            await ReportSuspicionsAsync();
            return false;
        }

        // Proceed with execution logic here
        Console.WriteLine($"Query validated for tenant {tenantId}.");
        return true;
    }

    private async Task ReportSuspicionsAsync()
    {
        var suspicions = await _verifier.DetectPotentialDataLeaksAsync();
        
        foreach (var suspicion in suspicions.Where(s => s.Severity == "Critical"))
        {
            Console.WriteLine($"ALERT: {suspicion.Type} - {suspicion.Description}");
        }
    }
}
```

## Notes

*   **Thread Safety**: The `TenantIsolationVerifier` class is sealed and stateless regarding external inputs, but its internal database connections should be treated as non-thread-safe unless the underlying SQLite provider is explicitly configured for multi-threaded access. It is recommended to instantiate a new verifier per logical operation or ensure serialized access when calling `VerifyTenantIsolationAsync` and `DetectPotentialDataLeaksAsync` concurrently.
*   **Query Parsing Limitations**: The `ValidateQueryTenantIsolationAsync` method performs static analysis on the query string. It may not detect complex tenant filters hidden within stored procedures, views, or dynamically constructed SQL strings that are not passed directly as the `query` parameter.
*   **False Positives in Leak Detection**: The `DetectPotentialDataLeaksAsync` method relies on heuristic analysis of data patterns. Results marked as "Low" or "Medium" severity in `DataLeakageSuspicion` should be investigated manually, as they may represent edge cases in legitimate data structures rather than actual breaches.
*   **Timestamp Consistency**: The `VerifiedAt` property in `IsolationVerificationResult` reflects the completion time of the verification task. In high-concurrency environments, the state of the database may change immediately after this timestamp, requiring frequent re-verification for critical security boundaries.
