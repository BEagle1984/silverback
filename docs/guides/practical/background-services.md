---
uid: background-services
---

# Background Services with a Distributed Lock

When an application is scaled out to several instances, some background tasks must still run on a single node at a time: a periodic cleanup, a data export, a polling job, etc. Silverback uses a distributed lock to ensure that only one instance of the outbox worker is active, and the same building blocks are available to implement your own [hosted services](https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services).

## Base Classes

Two base classes are available in the `Silverback.Background` namespace ([Silverback.Core](https://www.nuget.org/packages/Silverback.Core)):

- <xref:Silverback.Background.DistributedBackgroundService> acquires the lock and then calls `ExecuteLockedAsync` once. The lock is released when the method returns.
- <xref:Silverback.Background.RecurringDistributedBackgroundService> acquires the lock and calls `ExecuteLockedAsync` repeatedly, waiting for the specified interval between two executions. The lock is held for as long as the instance keeps running.

Both classes derive from `BackgroundService` and receive an <xref:Silverback.Lock.IDistributedLock> in their constructor. The lock is obtained from the <xref:Silverback.Lock.IDistributedLockFactory>, passing the settings of the desired lock implementation.

## Implement the Service

The following example runs a cleanup task every 5 minutes, on one instance at a time, using a PostgreSQL advisory lock.

```csharp
public class CleanupService : RecurringDistributedBackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CleanupService(
        IDistributedLockFactory distributedLockFactory,
        IServiceProvider serviceProvider,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ISilverbackLogger<CleanupService> logger)
        : base(
            TimeSpan.FromMinutes(5),
            distributedLockFactory.GetDistributedLock(
                new PostgreSqlAdvisoryLockSettings("cleanup", configuration.GetConnectionString("Default")!),
                serviceProvider),
            logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteLockedAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            ICleanupJob job = scope.ServiceProvider.GetRequiredService<ICleanupJob>();

            await job.RunAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log and swallow the exception, to keep the recurring service alive
        }
    }
}
```

## Register the Lock and the Service

The lock implementation must be registered in Silverback, and the service must be registered as a hosted service.

# [PostgreSQL](#tab/postgresql)
```csharp
services
    .AddSilverback()
    .AddPostgreSqlAdvisoryLock();

services.AddHostedService<CleanupService>();
```
# [Entity Framework](#tab/ef)
```csharp
services
    .AddSilverback()
    .AddEntityFrameworkLock();

services.AddHostedService<CleanupService>();
```
***

Each storage package provides its own lock settings. You can either instantiate the settings record directly or use the same builder that is used to configure the outbox worker:

| Lock | Settings | Registration |
|------|----------|--------------|
| PostgreSQL advisory lock | `new PostgreSqlAdvisoryLockSettings(lockName, connectionString)` or `new DistributedLockSettingsBuilder().UsePostgreSqlAdvisoryLock(lockName, connectionString).Build()` | `AddPostgreSqlAdvisoryLock()` |
| PostgreSQL table-based lock | `new DistributedLockSettingsBuilder().UsePostgreSqlTable(lockName, connectionString).Build()` | `AddPostgreSqlTableLock()` |
| Entity Framework table-based lock | `new DistributedLockSettingsBuilder().UseDbContext<TDbContext>(lockName).Build()` | `AddEntityFrameworkLock()` |
| In-memory lock (single process only) | `new InMemoryLockSettings(lockName)` | `AddInMemoryLock()` |

> [!Note]
> The table-based locks require a locks table. With Entity Framework, add a `DbSet<SilverbackLock>` to your `DbContext` and create the table via migrations. For PostgreSQL, the table can be created with `CreatePostgreSqlLocksTableAsync`. See <xref:storage> for details.

> [!Tip]
> In the tests, call `UseInMemoryLock()` to replace every distributed lock with the in-memory implementation, without changing the service code.

Passing `null` settings to `GetDistributedLock` returns a no-op lock: the task will then run on every instance.

## Key Points

- **Lock name**: use a dedicated lock name per task. All instances executing the same task must use the same name, while unrelated tasks must use different names to avoid blocking each other.
- **Interval**: in the recurring service, the interval is the delay between the end of an execution and the start of the next one, not a fixed schedule.
- **Lock loss**: in the recurring service, the token passed to `ExecuteLockedAsync` is canceled when the service is stopping or the lock is lost (e.g., the database connection drops or a heartbeat fails). The service then tries to acquire the lock again. Always forward the token to your long-running operations.
- **Pause and resume**: <xref:Silverback.Background.RecurringDistributedBackgroundService> exposes the `Pause()` and `Resume()` methods to temporarily suspend the execution without releasing the lock.
- **Scoped services**: hosted services are singletons. Create a scope via `IServiceScopeFactory` to resolve scoped dependencies such as a `DbContext`.

> [!Important]
> An exception thrown by `ExecuteLockedAsync` (other than a cancellation) is logged and stops the service: the recurring task will not be executed anymore until the application is restarted. Handle the exceptions inside `ExecuteLockedAsync` if the task must keep running, as shown in the example above.

> [!Warning]
> The base <xref:Silverback.Background.DistributedBackgroundService> passes only the stopping token to `ExecuteLockedAsync` and doesn't react to a lost lock. For long-running work, prefer the recurring service or override `AcquireLockAndExecuteAsync` to observe the `LockLostToken` of the <xref:Silverback.Lock.DistributedLockHandle>.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:storage>
- <xref:outbox>
