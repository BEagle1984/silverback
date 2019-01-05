---
title: Outbound Connector
permalink: /docs/configuration/outbound
---

The outbound connector is used to automatically relay the integration messages (published to the internal bus) to the message broker. Multiple outbound endpoints can be configured and Silverback will route the messages according to their type (based on the `TMessage` parameter passed to the `AddOutbound<TMessage>` method.

# Implementations

Multiple implementations of the connector are available, offering a variable degree of reliability.

## Basic

The basic `OutboundConnector` is very simple and relays the messages synchronously. This is the easiest, better performing and most lightweight option but it doesn't allow for any transactionality (once the message is fired, is fired) nor resiliency to the message broker failure.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddOutboundConnector());
    ...
}

public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
        .AddOutbound<IIntegrationEvent>(
            new KafkaEndpoint("basket-events")
            {
                ...
            })
        .Broker.Connect();
```

## Deferred

The `DeferredOutboundConnector` will store the outbound messages into a database table and produce them asynchronously. This allows to take advantage of database transactions, preventing inconsistencies. And in addition allows the system to retry indefinitely if the message broker is not available.

The **Silverback.Integration.EntityFrameworkCore** package contains an implementation that allows to store the outbound messages into a DbSet, being therefore implicitly saved in the same transaction used to save changes to the local data.

The `DbContext` must include a `DbSet<OutboundMessage>` and an `OutboundWorker` is to be scheduled using your scheduler of choice to process the outbound queue.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddDbOutboundConnector<MyDbContext>()
            .AddDbOutboundWorker<MyDbContext>());
    ...
}

public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints, JobScheduler jobScheduler)
{
    endpoints
        .AddOutbound<IIntegrationEvent>(
            new KafkaEndpoint("catalog-events")
            {
                ...
            })
        .Broker.Connect();

    // Scheduling the OutboundQueueWorker using a poor-man scheduler
    jobScheduler.AddJob("outbound-queue-worker", TimeSpan.FromMilliseconds(100),
        s => s.GetRequiredService<OutboundQueueWorker>().ProcessQueue());
```