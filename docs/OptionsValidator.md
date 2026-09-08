# `OptionsValidator`

`OptionsValidator` is a static configuration-validation helper in
`SqliteMultiTenant.Configuration`. Each overload throws an
`ArgumentNullException` when its options argument is `null` and an
`ArgumentException` when a checked property does not meet its constraint.

## Public methods

### `Validate(MultiTenantOptions options)`

```csharp
public static void Validate(MultiTenantOptions options)
```

Validates these `MultiTenantOptions` properties:

| Property | Constraint |
| --- | --- |
| `BasePath` | Must not be `null`, empty, or consist only of white-space characters. |
| `MaxConnectionsPerTenant` | Must be greater than `0`. |
| `MaxBackupCount` | Must be greater than `0`. |
| `BackupRetention` | Must be greater than `TimeSpan.Zero`. |

No other `MultiTenantOptions` properties are checked by this method.

### `Validate(BackupOptions options)`

```csharp
public static void Validate(BackupOptions options)
```

Validates these `BackupOptions` properties:

| Property | Constraint |
| --- | --- |
| `MaxConcurrentBackups` | Must be greater than `0`. |
| `BackupTimeoutSeconds` | Must be greater than `0`. |

### `Validate(SecurityOptions options)`

```csharp
public static void Validate(SecurityOptions options)
```

Validates these `SecurityOptions` properties:

| Property | Constraint |
| --- | --- |
| `SessionTimeout` | Must be greater than `TimeSpan.Zero`. |
| `MaxFailedLoginAttempts` | Must be greater than `0`. |
| `LockoutDuration` | Must be greater than `TimeSpan.Zero`. |

## Usage example

```csharp
using System;
using SqliteMultiTenant.Configuration;

var options = new MultiTenantOptions
{
    BasePath = "./tenant-databases",
    MaxConnectionsPerTenant = 10,
    MaxBackupCount = 20,
    BackupRetention = TimeSpan.FromDays(30)
};

OptionsValidator.Validate(options);
```

Successful validation returns normally. Invalid configuration throws at the
first failing check; for example, setting `MaxBackupCount` to `0` causes an
`ArgumentException` with the message `MaxBackupCount must be greater than 0`.
