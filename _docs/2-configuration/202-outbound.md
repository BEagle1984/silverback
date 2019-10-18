---
title: Outbound Connector
permalink: /docs/configuration/outbound
---

The outbound connector is used to automatically relay the integration messages (published to the internal bus) to the message broker. Multiple outbound endpoints can be configured and Silverback will route the messages according to their type (based on the `TMessage` parameter passed to the `AddOutbound<TMessage>` method.

## Implementations

Multiple implementations of the connector are available, offering a variable degree of reliability.

### Basic

The basic `OutboundConnector` is very simple and relays the messages synchronously. This is the easiest, better performing and most lightweight option but it doesn't allow for any transactionality (once the message is fired, is fired) nor resiliency to the message broker failure.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddSilverback()
        .WithConnectionTo<KafkaBroker>(options => options
            .AddOutboundConnector());
    ...
}
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("basket-events")
                {
                    ...
                }));
```

### Deferred

The `DbOutboundConnector` will store the outbound messages into a database table and produce them asynchronously. This allows to take advantage of database transactions, preventing inconsistencies. And in addition allows the system to retry indefinitely if the message broker is not available.

When using entity framework (`UseDbContext<TDbContext>`) the outbound messages are stored into a DbSet and are therefore implicitly saved in the same transaction used to save all other changes.

The `DbContext` must include a `DbSet<OutboundMessage>` and an `OutboundWorker` is to be started to process the outbound queue.

**Important!** The current `OutboundWorker` cannot be horizontally scaled and starting multiple instances will cause the messages to be produced multiple times. In the following example a distributed lock in the database is used to ensure that only one instance is running and another one will _immediatly_ take over when it stops (the `DbContext` must include a `DbSet<Lock>` as well).
{: .notice--warning}

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddSilverback()

        // Initialize Silverback to use MyDbContext as database storage.
        .UseDbContext<MyDbContext>()

        // Setup the lock manager using the database
        // to handle the distributed locks.
        // If this line is omitted the OutboundWorker will still
        // work without locking. 
        .AddDbDistributedLockManager()

        .WithConnectionTo<KafkaBroker>(options => options
            // Use a deferred outbound connector
            .AddDbOutboundConnector()

            // Add the IHostedService processing the outbound queue
            // (overloads are available to specify custom interval, lock timeout, etc.)
            .AddDbOutboundWorker();
    ...
}
```

#### Extensibility

You can easily create another implementation targeting another kind of storage, simply creating your own `IOutboundQueueProducer` and `IOutboundQueueConsumer`.
It is then suggested to create an extension method for the `BrokerOptionsBuilder` to register your own types.

```c#
public static BrokerOptionsBuilder AddMyCustomOutboundConnector(this BrokerOptionsBuilder builder)
{
    builder.AddOutboundConnector<DeferredOutboundConnector>();
    builder.Services.AddScoped<IOutboundQueueProducer, MyCustomQueueProducer>();

    return builder;
}

public static BrokerOptionsBuilder AddMyCustomOutboundWorker(this BrokerOptionsBuilder builder,
    bool enforceMessageOrder = true, int readPackageSize = 100)
{
    builder.AddOutboundWorker(enforceMessageOrder, readPackageSize);
    builder.Services.AddScoped<IOutboundQueueConsumer, MyCustomQueueConsumer>();

    return builder;
}
```