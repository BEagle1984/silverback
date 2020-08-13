---
uid: inbound
---

# Inbound Connector

The inbound connector is used to automatically consume a topic/queue and relay the messages to the internal bus.

> [!Note]
> The inbound connector abstracts the message broker completely and the messages are automatically acknowledged if the subscribers complete without throwing an exception (unless error handling policies are defined and unless batch processing).

## Implementations

Multiple implementations are available, offering a variable degree of reliability.

### Basic

The basic `InboundConnector` is very simple and just forwards the consumed messages to the internal bus. If no exception is thrown, the message is committed and the next one is consumed.

<figure>
	<a href="~/images/diagrams/inbound-basic.png"><img src="~/images/diagrams/inbound-basic.png"></a>
    <figcaption>The messages are consumed directly.</figcaption>
</figure>

# [Startup](#tab/basic-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```
# [EndpointsConfigurator](#tab/basic-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("basket-events")
                {
                    ...
                }));
    }
}
```
***

### Exactly-once processing

Silverback is able to keep track of the messages that have been consumed in order to guarantee that each message is processed exactly once.

#### Offset storage

The `DbOffsetStoredInboundConnector` will store the offset of the latest processed message (of each topic/partition) into a database table.

<figure>
	<a href="~/images/diagrams/inbound-offsetstore.png"><img src="~/images/diagrams/inbound-offsetstore.png"></a>
    <figcaption>The offsets are being stored to prevent the very same message to be consumed twice.</figcaption>
</figure>

> [!Note]
> The `Silverback.EntityFrameworkCore` package is also required and the `DbContext` must include a `DbSet<StoredOffset>`. See also the <xref:dbcontext>.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddDbOffsetStoredInboundConnector())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```

#### Logged

The `DbLoggedInboundConnector` will store all the processed messages into a database table. This has the double purpose of serving as a log in addition to preventing double processing.

<figure>
	<a href="~/images/diagrams/inbound-log.png"><img src="~/images/diagrams/inbound-log.png"></a>
    <figcaption>The inbound messages are logged to prevent two messages with the same key to be consumed.</figcaption>
</figure>

> [!Note]
> The `Silverback.EntityFrameworkCore` package is also required and the `DbContext` must include a `DbSet<InboundMessage>`. See also the <xref:dbcontext>.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .UseDbContext<MyDbContext>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddDbLoggedInboundConnector())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```

#### Custom store

You can easily implement your own storage for the offsets or the messages, simply creating your own `IOffsetStore` or `IInboundLog` and plugging them in.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddLoggedInboundConnector<SomeCustomInboundLog>()
                .AddOffsetStoredInboundConnector<SomeCustomOffsetStore>())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
}
```

## Error handling

If an exceptions is thrown by the methods consuming the incoming messages (subscribers) the consumer will stop, unless some error policies are defined.

Policy | Description
:-- | :--
`Skip` | This is the simplest policy: just ignore the message and go ahead. Use the `LogWithLevel` method to specify the log level to be applied the "message skipped" log entry (default is `Error`).
`Retry` | Define how many times and at which interval to retry to process the message. Be aware that this will block the consumer.
`Move` | Used to re-publish the message to the specified endpoint, this policy is very flexible and allow quite a few scenarios: move to same topic to retry later on without blocking, move to a retry topic to delay the retry or move to a failed messages topic. The message can also be transformed, to allow adding useful information (e.g. source, error type, etc.) that will allow for better handling while reprocessing.
`Chain` | Combine different policies, for example to move the message to a dead letter after some retries.

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder.AddInbound(
            new KafkaConsumerEndpoint("some-events")
            {
                ...
            },
            policy => policy.Chain(
                policy.Retry().MaxFailedAttempts(3),
                policy.Move(new KafkaProducerEndpoint("bad-messages")
                    {
                        ...
                    }
                )));
    }
}
```

> [!Important]
> If the processing still fails after the last policy is applied the inbound connector will return the exception to the consumer, causing it to stop. A `Retry` (with limited amount of attempts) alone is therefore not recommendend and it should be combined with `Skip` or `Move`.

### Retries

`Retry` and `Move` policies can be used to retry over and over the same message. Use `MaxFailedAttempts` to limit the number of attempts.

```csharp
policy.Chain(
    policy.Retry(TimeSpan.FromSeconds(1)).MaxFailedAttempts(3),
    policy.Skip().LogWithLevel(LogLevel.Critical))
```

> [!Note]
> A message can be moved to the same topic to simply be moved to the end of the queue.

> [!Important]
> The Retry policy will prevent the message broker to be polled for the entire comulative duration of the attempts and it could lead to timeouts. With Kafka you should for example set the `max.poll.interval.ms` settings to an higher value.

### Apply rules

Use `ApplyTo` and `Exclude` methods to decide which exceptions must be handled by the error policy or take advantage of  `ApplyWhen` to specify a custom apply rule.

```csharp
policy.Move(new KafkaProducerEndpoint("same-endpoint") { ... })
    .Exclude<MyException>()
    .ApplyWhen((msg, ex) => msg.Xy == myValue)
```

### Publishing messages (events)

Messages can be published when a policy is applied, in order to execute custom code.

# [EndpointsConfigurator](#tab/eventhandler-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder.AddInbound(
            new KafkaConsumerEndpoint("some-events")
            {
                ...
            },
            policy => policy.Chain(
                policy
                    .Retry(TimeSpan.FromMilliseconds(500))
                    .MaxFailedAttempts(3),
                policy
                    .Skip()
                    .Publish(msg => new ProcessingFailedEvent(msg))
            ));
    }
}
```
# [Event Handler](#tab/eventhandler)

```csharp
public void OnProcessingFailed(ProcessingFailedEvent @event)
{
    _processingStatusService.SetFailed(@event.Message.Id);

    _mailService.SendNotification("Failed to process message!");
}
```
***

## Batch processing

The inbound connector can be configured to process the messages in batches.

<figure>
	<a href="~/images/diagrams/inbound-batch.png"><img src="~/images/diagrams/inbound-batch.png"></a>
    <figcaption>The messages are processed in batches.</figcaption>
</figure>

Property | Description
:-- | :--
`Batch.Size` | The number of messages to be processed in batch. The default is 1.
`Batch.MaxWaitTime` | The maximum amount of time to wait for the batch to be filled. After this time the batch will be processed even if the desired Size is not reached. Set it to `TimeSpan.MaxValue` to disable this feature. The default is `TimeSpan.MaxValue`.

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder.AddInbound(
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
}
```

> [!Note]
> The batch is consider a unit of work: it will be processed in the same DI scope, it will be atomically committed, the error policies will be applied to the batch as a whole and all messages will be acknowledged at once when the batch is successfully processed.

Some additional events are published to the internal bus when batch processing:

Event | Description
:-- | :--
`BatchStartedEvent` | Fired when the batch has been filled and just before the first message is published. This event can be subscribed to perform some operations before the messages are processed.
`BatchCompleteEvent` | Fired when all the messages in a batch have been published. 
`BatchProcessedEvent` | Fired after all messages have been successfully processed. It can tipically be used to commit the transaction.
`BatchAbortedEvent` | Fired when an exception occured during the processing of the batch. It can tipically be used to rollback the transaction.

The usage should be similar to the following examples.

# [Example 1](#tab/batch-example1)
```csharp
public class InventoryService
{
    private DbContext _db;

    public InventoryService(MyDbContext db)
    {
        _db = db;
    }

    public void OnBatchStarted(BatchStartedEvent message)
    {
        _logger.LogInformation(
            $"Processing batch '{message.BatchId} " +
            $"({message.BatchSize} messages)");
    }

    public void OnMessageReceived(InventoryUpdateEvent @event)
    {
        // Process the event (but don't call SaveChanges)
    }

    public async Task OnBatchProcessed(BatchProcessedEvent message)
    {
        // Commit all changes in a single transaction
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            $"Successfully processed batch '{message.BatchId} " +
            $"({message.BatchSize} messages)");
    }
    
    public void OnBatchAborted(BatchAbortedEvent message)
    {
        _logger.LogError(
            $"An error occurred while processing batch '{message.BatchId} " +
            $"({message.BatchSize} messages)");
    }
}
```
# [Startup](#tab/batch-example2)
```csharp
public class InventoryService
{
    private DbContext _db;

    public InventoryService(MyDbContext db)
    {
        _db = db;
    }

    public void OnBatchStarted(BatchStartedEvent message)
    {
    }

    public async Task OnMessageReceived(
        IReadOnlyCollection<InventoryUpdateEvent> events)
    {
        _logger.LogInformation(
            $"Processing {events.Count} messages");

        // Process all items
        foreach (var event in events)
        {
            ...
        }

        // Commit all changes in a single transaction
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            $"Successfully processed {events.Count} messages");
    }
}
```
***

> [!Note]
> The method `OnMessageReceived` could declare an argument of type `IReadOnlyCollection<InventoryUpdateEvent>` instead of `IEnumerable<InventoryUpdateEvent>`. (Silverback will in any case always forward a materialized `IList` of messages, but explicitly declaring the paramter as `IReadOnlyCollection<T>` avoids any false positive *"possible multiple enumeration of IEnumerable"* issue that may be detected by a static code analysis tool.)

## Multi-threaded consuming

Multiple consumers can be created for the same endpoint to consume in parallel in multiple threads (you need multiple partitions in Kafka).

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder)
    {
        builder.AddInbound(
            new KafkaConsumerEndpoint("basket-events")
            {
                ...
            },
            settings: new InboundConnectorSettings
            {
                Consumers: 2
            }));
    }
}
```