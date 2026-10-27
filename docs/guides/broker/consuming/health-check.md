---
uid: consumer-health-check
---

# Consumer Health Check

The [Silverback.Integration.HealthChecks](https://www.nuget.org/packages/Silverback.Integration.HealthChecks/) package integrates with the [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) to monitor the consumers and report them as unhealthy if they aren't connected or consuming.

```powershell
dotnet add package Silverback.Integration.HealthChecks
```

## Registering the Check

Add the consumers check to the health checks builder:

```csharp
services
    .AddHealthChecks()
    .AddConsumersCheck();
```

The check verifies that every consumer reached at least the minimum healthy status. A consumer whose status changed recently is not reported as unhealthy until the grace period has elapsed, to tolerate the transient states during the startup and the rebalances.

## Options

The `AddConsumersCheck` method accepts the following optional parameters:

Parameter | Default | Description
:-- | :-- | :--
`minHealthyStatus` | `ConsumerStatus.Connected` | The minimum <xref:Silverback.Messaging.Broker.ConsumerStatus> a consumer must have to be considered healthy (`Stopped`, `Started`, `Connected` or `Consuming`).
`gracePeriod` | 30 seconds | The time to wait after each status change before a consumer is considered unhealthy.
`name` | `"Consumers"` | The name of the health check.
`failureStatus` | `HealthStatus.Unhealthy` | The status reported when the check fails.
`tags` | _none_ | The tags used to filter the health checks.

```csharp
services
    .AddHealthChecks()
    .AddConsumersCheck(
        minHealthyStatus: ConsumerStatus.Consuming,
        gracePeriod: TimeSpan.FromMinutes(1),
        tags: ["ready"]);
```

> [!Tip]
> Requiring `ConsumerStatus.Consuming` makes the check stricter: a consumer is considered healthy only after it received at least one message. Use it only when messages are expected to flow continuously.

## Outbox Check

A similar check is available to monitor the outbox (`AddOutboxCheck`), verifying that the messages are being produced and the queue isn't growing too much. See <xref:outbox>.

## Additional Resources

- <xref:Silverback.Messaging.HealthChecks.HealthCheckBuilderExtensions>
- <xref:consuming>
- <xref:outbox>
