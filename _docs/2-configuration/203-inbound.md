---
title: Inbound Connector
permalink: /docs/configuration/inbound
---

The inbound connector is used to automatically consume a topic/queue and relay the messages to the internal bus.

# Implementations

Multiple implementations are available, offering a variable degree of reliability.

## Basic

The basic `InboundConnector` is very simple and just forwards the consumed messages to the internal bus. If no exception is thrown, the message is committed and the next one is consumed.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddInboundConnector());
    ...
}

public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
        .AddInbound(
            new KafkaEndpoint("basket-events")
            {
                ...
            })
        .Broker.Connect();
```

## Logged (exactly-once processing)

This `LoggedInboundConnector` will store the unique identifier of the processed messages into a database table to prevent double processing.

The `DbContext` must include a `DbSet<InboundMessage>`.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddDbInboundConnector<MyDbContext>());
    ...
}

public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
        .AddInbound(
            new KafkaEndpoint("basket-events")
            {
                ...
            })
        .Broker.Connect();
```

# Error Handling

If an exceptions is thrown by the methods consuming the incoming messages (subscribers) the consumer will stop, unless some error policies are defined.

Policy | Description
:-- | :--
Skip | This is the simplest policy: just ignore the message and go ahead.
Retry | Define how many times and at which interval to retry to process the message. Be aware that this will block the consumer.
Move | Used to re-publish the message to the specified endpoint, this policy is very flexible and allow quite a few scenarios: move to same topic to retry later on without blocking, move to a retry topic to delay the retry or move to a failed messages topic. The message can also be transformed, to allow adding useful information (e.g. source, error type, etc.) that will allow for better handling while reprocessing.
Chain | Combine different policies, for example to move the message to a dead letter after some retries.

```c#
protected override void Configure(IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
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
                )))
        .Broker.Connect();

}
```

Use `ApplyTo` and `Exclude` methods to decide which exceptions must be handled by the error policy or take advantage of  `ApplyWhen` to specify a custom apply rule.

```c#
policy.Move(new KafkaProducerEndpoint("same-endpoint") { ... })
    .Exclude<MyException>()
    .ApplyWhen((msg, ex) => msg.FailedAttempts < 10)
```

# Batch Processing

The inbound connector can be configured to process the messages in batches.

Property | Description
:-- | :--
Batch.Size | The number of messages to be processed in batch. The default is 1.
Batch.MaxWaitTime | The maximum amount of time to wait for the batch to be filled. After this time the batch will be processed even if the desired Size is not reached. Set it to `TimeSpan.MaxValue` to disable this feature. The default is `TimeSpan.MaxValue`.
Batch.MaxDegreeOfParallelism | The maximum number of parallel threads used to process the messages in the batch. The default is 1.

```c#
public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
        .AddInbound(
            new KafkaEndpoint("basket-events")
            {
                ...
            },
            settings: new InboundConnectorSettings
            {
                Batch = new Messaging.Batch.BatchSettings
                {
                    Size = 5,
                    MaxDegreeOfParallelism = 2,
                    MaxWaitTime = TimeSpan.FromSeconds(5)
                }
            }))
        .Broker.Connect();
```

**Important*! The batch is consider a unit of work: it will be processed in the same DI scope, it will be atomically committed and the error policies will be applied to the batch as a whole.{: .notice--info}

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

# Multi-threading

Multiple consumers can be created for the same endpoint to consume in parallel in multiple threads (you need multiple partitions in Kafka).

```c#
public void Configure(..., IBrokerEndpointsConfigurationBuilder endpoints)
{
    endpoints
        .AddInbound(
            new KafkaEndpoint("basket-events")
            {
                ...
            },
            settings: new InboundConnectorSettings
            {
                Consumers: 2
            }))
        .Broker.Connect();
```