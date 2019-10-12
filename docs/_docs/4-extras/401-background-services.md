---
title: Distributed Background Services
permalink: /docs/extras/background-services

toc: false
---

To implement the `OutboundWorker` we had to create a database based locking mechanism, to ensure that only a single instance of our worker was running. You can take advantage of this implementation to build your `IHostedService`.

Two base classes are available in Silverback.Core: `DistributedBackgroundWorkerService` implements the basic locking mechanism, while `RecurringDistributedBackgroundService` adds on top of it the ability to run a task as specified intervals.

```c#
using Silverback.Background;

namespace Silverback.Messaging.Connectors
{
    public class MyBackroundService : RecurringDistributedBackgroundService
    {
        private readonly IMyService _myService;

        public OutboundQueueWorkerService(IMyService _myService, IDistributedLockManager distributedLockManager, ILogger<OutboundQueueWorkerService> logger)
            : base(
                TimeSpan.FromMinutes(5), // interval
                new DistributedLockSettings
                {
                    AcquireRetryInterval = TimeSpan.FromMinutes(5)
                }, 
                distributedLockManager, logger)
        {
        }

        protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) => 
            _myService.DoWork(stoppingToken);
    }
}
```