# DataRetentionPolicy

`DataRetentionPolicy` is a sealed class that orchestrates the enforcement of time-based data retention rules across multiple tenant-specific tables. It evaluates a configurable set of `RetentionRule` definitions, optionally archives records before deletion, and returns a detailed `RetentionResult` summarizing the operation.

## API

### DataRetentionPolicy

```csharp
public DataRetentionPolicy()
```

Default constructor. Initializes a new instance without any pre-configured rules. The instance must be supplied with a `RetentionPolicyConfig` before calling `ApplyRetentionPolicyAsync`.

---

### ApplyRetentionPolicyAsync

```csharp
public async Task<RetentionResult> ApplyRetentionPolicyAsync(RetentionPolicyConfig config)
```

Executes all enabled retention rules defined in the provided configuration against the corresponding tenant database.

**Parameters:**
- `config` — A `RetentionPolicyConfig` object specifying the tenant identifier, the collection of retention rules, and whether automatic execution is indicated.

**Returns:**
- A `Task<RetentionResult>` whose result indicates overall success, the total number of records deleted across all rules, and the timestamp of execution.

**Exceptions:**
- Throws `ArgumentNullException` when `config` is `null`.
- Throws `InvalidOperationException` when `config.TenantId` is `null`, empty, or consists only of whitespace.
- Throws `InvalidOperationException` when `config.Rules` is `null` or contains no enabled rules.
- Exceptions from underlying database access (e.g., connection failures, SQL execution errors) propagate to the caller.

---

### GetDefaultPolicy

```csharp
public static RetentionPolicyConfig GetDefaultPolicy(string tenantId)
```

Produces a baseline `RetentionPolicyConfig` for a given tenant with a pre-defined set of common retention rules. The returned configuration has `AutoExecute` set to `false`.

**Parameters:**
- `tenantId` — The tenant identifier for which the default policy is generated.

**Returns:**
- A `RetentionPolicyConfig` populated with default rules.

**Exceptions:**
- Throws `ArgumentNullException` when `tenantId` is `null`.
- Throws `ArgumentException` when `tenantId` is empty or whitespace.

---

### RetentionPolicyConfig

```csharp
public sealed class RetentionPolicyConfig
```

Holds the complete retention policy definition for a single tenant.

**Members:**

- `public string TenantId` — The unique identifier of the tenant to which this policy applies.
- `public List<RetentionRule> Rules` — The collection of retention rules to evaluate. Rules with `IsEnabled == false` are skipped during execution.
- `public bool AutoExecute` — When `true`, signals that the policy is intended for automated, recurring execution. The `ApplyRetentionPolicyAsync` method itself does not use this flag to alter behavior; it is provided for external schedulers.

---

### RetentionRule

```csharp
public sealed class RetentionRule
```

Defines a single retention directive for a specific table.

**Members:**

- `public string TableName` — The name of the table from which records should be removed.
- `public string DateColumn` — The name of the column used to determine record age. Must be a date or datetime column.
- `public RetentionType RetentionType` — The unit of time used for the retention threshold (e.g., Days, Months, Years).
- `public int RetentionValue` — The magnitude of the retention period. Combined with `RetentionType`, this defines the cutoff: records older than this threshold are subject to removal.
- `public bool IsEnabled` — If `false`, the rule is ignored during policy execution.
- `public bool ArchiveBeforeDelete` — When `true`, records that would be deleted are first copied to the table specified by `ArchiveTableName`.
- `public string ArchiveTableName` — The target table for archiving. Required when `ArchiveBeforeDelete` is `true`; otherwise ignored.

---

### RetentionResult

```csharp
public sealed class RetentionResult
```

Contains the outcome of a retention policy execution.

**Members:**

- `public bool IsSuccessful` — Indicates whether the entire operation completed without errors. If any rule fails, this is `false`.
- `public int TotalRecordsDeleted` — The sum of all records deleted across all successfully executed rules. Records that were archived before deletion are counted here.
- `public DateTime ExecutedAt` — The UTC timestamp when the retention execution began.

---

### RetentionType

An enum (referenced by `RetentionRule`) specifying the time unit for retention thresholds. Values include `Days`, `Months`, and `Years`.

## Usage

### Example 1: Manual One-Time Cleanup with Archiving

```csharp
var policy = new DataRetentionPolicy();

var config = new RetentionPolicyConfig
{
    TenantId = "tenant-42",
    AutoExecute = false,
    Rules = new List<RetentionRule>
    {
        new RetentionRule
        {
            TableName = "Orders",
            DateColumn = "CreatedAt",
            RetentionType = RetentionType.Months,
            RetentionValue = 6,
            IsEnabled = true,
            ArchiveBeforeDelete = true,
            ArchiveTableName = "Orders_Archive"
        },
        new RetentionRule
        {
            TableName = "Logs",
            DateColumn = "Timestamp",
            RetentionType = RetentionType.Days,
            RetentionValue = 90,
            IsEnabled = true,
            ArchiveBeforeDelete = false
        }
    }
};

RetentionResult result = await policy.ApplyRetentionPolicyAsync(config);

Console.WriteLine($"Success: {result.IsSuccessful}");
Console.WriteLine($"Deleted: {result.TotalRecordsDeleted}");
Console.WriteLine($"Executed: {result.ExecutedAt}");
```

### Example 2: Using Default Policy with Scheduled Execution Flag

```csharp
var policy = new DataRetentionPolicy();

// Obtain a baseline policy and mark it for automated execution.
RetentionPolicyConfig defaultConfig = DataRetentionPolicy.GetDefaultPolicy("tenant-99");
defaultConfig.AutoExecute = true;

// Optionally disable a rule that is not relevant.
var auditRule = defaultConfig.Rules.FirstOrDefault(r => r.TableName == "AuditTrail");
if (auditRule != null)
    auditRule.IsEnabled = false;

RetentionResult result = await policy.ApplyRetentionPolicyAsync(defaultConfig);

if (!result.IsSuccessful)
{
    // Log failure and alert operations.
}
```

## Notes

- **Rule ordering:** Rules are executed sequentially in the order they appear in the `Rules` list. A failure in one rule causes the entire operation to be marked unsuccessful, but subsequent rules are still attempted.
- **Archiving:** When `ArchiveBeforeDelete` is `true`, the archive table must exist and have a schema compatible with the source table. The method does not create the archive table automatically. If the archive table is missing or incompatible, the rule execution fails and `IsSuccessful` becomes `false`.
- **Date column requirements:** The column specified by `DateColumn` must be of a type that supports date comparison. Using a non-date column results in a SQL execution error.
- **Empty rules collection:** Supplying a configuration with zero enabled rules causes `ApplyRetentionPolicyAsync` to throw `InvalidOperationException`. At least one enabled rule is required.
- **Thread safety:** `DataRetentionPolicy` itself holds no mutable state and is safe to use concurrently. `RetentionPolicyConfig` and `RetentionRule` are plain data containers and are not thread-safe if mutated while being passed to `ApplyRetentionPolicyAsync`. Callers should ensure the configuration is fully prepared before execution.
- **Transactions:** Each rule executes within its own implicit transaction. A failure in one rule does not roll back the deletions performed by previously successful rules.
- **Tenant isolation:** The method relies on the caller to supply the correct `TenantId`. No cross-tenant data access checks are performed internally; the underlying database connection must be scoped to the target tenant.
