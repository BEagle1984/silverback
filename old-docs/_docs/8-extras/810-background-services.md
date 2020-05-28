---
title: Distributed Background Services
permalink: /docs/extras/background-services

toc: true
---

To implement the `OutboundWorker` we had to create a database based locking mechanism, to ensure that only a single instance of our worker was running. You can take advantage of this implementation to build your `IHostedService`.

## DistributedBackgroundService

Two base classes are available in `Silverback.Core`: `DistributedBackgroundService` implements the basic locking mechanism, while `RecurringDistributedBackgroundService` adds on top of it the ability to run a task as specified intervals.

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

A `DistributedLockSettings` object can be passed to the constructor of the base class to customize lock timeout, heartbeat interval, etc.
{: .notice--note}

## Lock Manager

To enable the distributed locks an `IDistributedLockManager` implementation (probably a `DbDistributedLockManager`) must be registered for dependency injection as shown in the next code snippet. 

The `Silverback.EntityFrameworkCore` package is also required and the `DbContext` must configure a `DbSet<Lock>`. See also the [sample DbContext]({{ site.baseurl }}/docs/extra/dbcontext).
{: .notice--note}

<figure class="csharp">
<figcaption>Startup.cs</figcaption>
{% highlight csharp %}
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
{% endhighlight %}
</figure>

