# Distributed Background Services

To implement the <xref:Silverback.Messaging.Outbound.TransactionalOutbox.OutboxWorkerService> we had to create a database based locking mechanism, to ensure that only a single instance of our worker was running. You can take advantage of this implementation to build your [IHostedService](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice).

## DistributedBackgroundService

Two base classes are available in [Silverback.Core](https://www.nuget.org/packages/Silverback.Core): <xref:Silverback.Background.DistributedBackgroundService> implements the basic locking mechanism, while <xref:Silverback.Background.RecurringDistributedBackgroundService> adds on top of it the ability to run a task as specified intervals.

```csharp
using Silverback.Background;

namespace Sample
{
    public class MyBackroundService
        : RecurringDistributedBackgroundService
    {
        private readonly IMyService _myService;

        public MyBackroundService(
            IMyService _myService, 
            IDistributedLockManager distributedLockManager, 
            ILogger<OutboundQueueWorkerService> logger)
            : base(
                TimeSpan.FromMinutes(5), // interval
                distributedLockManager, logger)
        {
        }

        protected override Task ExecuteRecurringAsync(
            CancellationToken stoppingToken) => 
            _myService.DoWork(stoppingToken);
    }
}
```

> [!Note]
> A <xref:Silverback.Background.DistributedLockSettings> object can be passed to the constructor of the base class to customize lock timeout, heartbeat interval, etc.

## Lock Manager

To enable the distributed locks an <xref:Silverback.Background.IDistributedLockManager> implementation (probably a <xref:Silverback.Background.DbDistributedLockManager>) must be registered for dependency injection as shown in the next code snippet. 

> [!Note]
> The [Silverback.Core.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore) package is also required and the `DbContext` must configure a `DbSet<Lock>`. See also the <xref:dbcontext>.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .AddDbDistributedLockManager();
    }
}
```
