---
title: Inbound Connector
permalink: /docs/configuration/inbound
---

The inbound connector is used to automatically consume a topic/queue and relay the messages to the internal bus.

**Note:** The inbound connector abstracts the message broker completely and the messages are automatically acknowledged if the subscribers complete without throwing an exception (unless error handling policies are defined and unless batch processing).
{: .notice--info}

## Implementations

Multiple implementations are available, offering a variable degree of reliability.

### Basic

The basic `InboundConnector` is very simple and just forwards the consumed messages to the internal bus. If no exception is thrown, the message is committed and the next one is consumed.

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddInboundConnector());
}

public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddInbound(
                new KafkaConsumerEndpoint("basket-events")
                {
                    ...
                }));
}
```

### Exactly-once processing

Silverback is able to keep track of the messages that have been consumed in order to guarantee that each one is processed exactly once.

#### Offset storage

The `DbOffsetStoredInboundConnector` will store the offset of the latest processed message (of each topic/partition) into a database table.

**Note:** The `DbContext` must include a `DbSet<StoredOffset>`.
{: .notice--info}

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddDbOffsetStoredConnector<MyDbContext>());
}
``` 

#### Logged

The `DbLoggedInboundConnector` will store all the processed messages into a database table. This has the double purpose of serving as a log in addition to preventing double processing.

**Note:** The `DbContext` must include a `DbSet<InboundMessage>`.
{: .notice--info}

```c#
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddDbLoggedInboundConnector<MyDbContext>());
}
```

#### Extensibility

You can easily implement your own storage for the offsets or the messages, simply creating your own `IOffsetStore` or `IInboundLog`.
It is then suggested to create an extension method for the `BrokerOptionsBuilder` to register your own types.

```c#
public static BrokerOptionsBuilder AddMyCustomLoggedInboundConnector<TDbContext>(this BrokerOptionsBuilder builder)
{
    builder.AddInboundConnector<LoggedInboundConnector>();
    builder.Services.AddScoped<IInboundLog, MyCustomInboundLog>();

    return builder;
}

public static BrokerOptionsBuilder AddMyCustomOffsetStoredInboundConnector<TDbContext>(this BrokerOptionsBuilder builder)
{
    builder.AddInboundConnector<OffsetStoredInboundConnector>();
    builder.Services.AddScoped<IOffsetStore, MyCustomOffsetStore>();

    return builder;
}
```

## Error handling

If an exceptions is thrown by the methods consuming the incoming messages (subscribers) the consumer will stop, unless some error policies are defined.

Policy | Description
:-- | :--
Skip | This is the simplest policy: just ignore the message and go ahead.
Retry | Define how many times and at which interval to retry to process the message. Be aware that this will block the consumer.
Move | Used to re-publish the message to the specified endpoint, this policy is very flexible and allow quite a few scenarios: move to same topic to retry later on without blocking, move to a retry topic to delay the retry or move to a failed messages topic. The message can also be transformed, to allow adding useful information (e.g. source, error type, etc.) that will allow for better handling while reprocessing.
Chain | Combine different policies, for example to move the message to a dead letter after some retries.

```c#
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddInbound(
                new KafkaConsumerEndpoint("some-events")
                {
                    ...
                },
                policy => policy.Chain(
                    policy.Retry(5, TimeSpan.FromSeconds(10)),
                    policy.Move(new KafkaProducerEndpoint("bad-messages")
                        {
                            ...
                        }
                    ))));
}
```

Use `ApplyTo` and `Exclude` methods to decide which exceptions must be handled by the error policy or take advantage of  `ApplyWhen` to specify a custom apply rule.

```c#
policy.Move(new KafkaProducerEndpoint("same-endpoint") { ... })
    .Exclude<MyException>()
    .ApplyWhen((msg, ex) => msg.FailedAttempts < 10)
```

**Important!** If the last applied policy still fails the inbound connector will return the exception to the consumer, causing it to stop. A _Retry_ alone is therefore not recommendend and it should be combined with _Skip_ or _Move_.
{: .notice--warning}

## Batch processing

The inbound connector can be configured to process the messages in batches.

Property | Description
:-- | :--
Batch.Size | The number of messages to be processed in batch. The default is 1.
Batch.MaxWaitTime | The maximum amount of time to wait for the batch to be filled. After this time the batch will be processed even if the desired Size is not reached. Set it to `TimeSpan.MaxValue` to disable this feature. The default is `TimeSpan.MaxValue`.
Batch.MaxDegreeOfParallelism | The maximum number of parallel threads used to process the messages in the batch. The default is 1.

```c#
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddInbound(
                new KafkaConsumerEndpoint("basket-events")
                {
                    ...
                },
                settings: new InboundConnectorSettings
                {
                    Batch = new Messaging.Batch.BatchSettings
                    {
                        Size = 5,
                        MaxWaitTime = TimeSpan.FromSeconds(5)
                    }
                }));
}
```

**Note:** The batch is consider a unit of work: it will be processed in the same DI scope, it will be atomically committed, the error policies will be applied to the batch as a whole and all messages will be acknowledged at once when the batch is successfully processed.
{: .notice--info}

Two additional events are published to the internal bus when batch processing:

Event | Description
:-- | :--
BatchReadyEvent | Fired when the batch has been filled and is ready to be processed. This event can be subscribed to perform some operations before the messages are processed or to implement all sorts of custom logics, having access to the entire batch.
BatchProcessedEvent | Fired after all messages have been successfully processed. It can tipically be used to commit the transaction.

The usage should be similar to the following example.

```c#
public class InventoryService : ISubscriber
{
    private DbContext _db;

    public InventoryService(MyDbContext db)
    {
        _db = db;
    }

    [Subscribe]
    void OnBatchReady(BatchReadyEvent message)
    {
        _logger.LogInformation($"Batch '{message.BatchId} ready ({message.BatchSize} messages)");
    }

    [Subscribe]
    void OnMessageReceived(MyMessage message)
    {
        ...
    }

    [Subscribe]
    void OnBatchProcessed(BatchProcessedEvent message)
    {
        _db.SaveChanges();

        _logger.LogInformation($"Successfully processed batch '{message.BatchId} ({message.BatchSize} messages)");
    }
}
```

## Multi-threaded consuming

Multiple consumers can be created for the same endpoint to consume in parallel in multiple threads (you need multiple partitions in Kafka).

```c#
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddInbound(
                new KafkaConsumerEndpoint("basket-events")
                {
                    ...
                },
                settings: new InboundConnectorSettings
                {
                    Consumers: 2
                }));
}
```
