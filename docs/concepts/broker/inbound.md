---
uid: inbound
---

# Inbound Endpoint

An inbound endpoint is used to configure Silverback to automatically consume a topic/queue and relay the messages to the internal bus. If no exception is thrown by the subscribers, the message is acknowledged and the next one is consumed.

The endpoint object identifies the topic/queue that is being connected and the client configuration, such the connection options. The endpoint object is therefore very specific and every broker type will define it's own implementation of [IConsumerEndpoint](xref:Silverback.Messaging.IConsumerEndpoint).

The options in the endpoint object are also used to tweak the Silverback behavior (e.g. the [deserialization](xref:serialization)) and to enable additional features such as [batch consuming](#batch-processing), [decryption](xref:encryption), etc.

> [!Note]
> Silverback abstracts the message broker completely and the messages are automatically acknowledged if the subscribers complete without throwing an exception.

## Apache Kafka

The <xref:Silverback.Messaging.KafkaConsumerEndpoint> is defined by
[Silverback.Integration.Kafka](https://www.nuget.org/packages/Silverback.Integration.Kafka) and is used to declare an inbound endpoint connected to Apache Kafka.

# [Fluent (preferred)](#tab/kafka-consumer-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events", "inventory-events")
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer";
                            config.AutoOffsetReset = AutoOffsetResetType.Earliest;
                        }
                    .OnError(policy => policy.Retry(5))));
}
```
# [Legacy](#tab/kafka-consumer-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint(
                    "order-events", 
                    "inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                        AutoOffsetReset = AutoOffsetResetType.Earliest
                    },
                    ErrorPolicy = new RetryErrorPolicy().MaxFailedAttempts(5) 
                });
}
```
***

> [!Note]
> You can decide whether to use one consumer per topic or subscribe multiple topics with the same consumer (passing multiple topic names in the endpoint constructor, as shown in the example above). There are advantages and disadvantages of both solutions and the best choice really depends on your specific requirements, the amount of messages being produced, etc. Anyway the main difference is that when subscribing multiple topics you will still consume one message after the other but they will simply be interleaved (this may or may not be an issue, it depends) and on the other hand each consumer will use some resources, so creating multiple consumers will result in a bigger overhead.

> [!Note]
> For a more in-depth documentation about the Kafka client configuration refer also to the [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html).

## RabbitMQ

[Silverback.Integration.RabbitMQ](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ) is a bit more intricate and uses 2 different classes to specify an endpoint that connects to a queue (<xref:Silverback.Messaging.RabbitQueueConsumerEndpoint>) or directly to an exchange (<xref:Silverback.Messaging.RabbitExchangeConsumerEndpoint>).

```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new RabbitQueueConsumerEndpoint("inventory-commands-queue")
                {
                    Connection = new RabbitConnectionConfig
                    {
                        HostName = "localhost",
                        UserName = "guest",
                        Password = "guest"
                    },
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = false,
                        IsAutoDeleteEnabled = false
                    }
                })
            .AddInbound(
                new RabbitExchangeConsumerEndpoint("order-events")
                {
                    Connection = new RabbitConnectionConfig
                    {
                        HostName = "localhost",
                        UserName = "guest",
                        Password = "guest"
                    },
                    Exchange = new RabbitExchangeConfig
                    {
                        IsDurable = true,
                        IsAutoDeleteEnabled = false,
                        ExchangeType = ExchangeType.Fanout
                    },
                    QueueName = "my-consumer-group",
                    Queue = new RabbitQueueConfig
                    {
                        IsDurable = true,
                        IsExclusive = false,
                        IsAutoDeleteEnabled = false
                    }
                });
}
```

> [!Note]
> For a more in-depth documentation about the RabbitMQ configuration refer to the [RabbitMQ tutorials and documentation](https://www.rabbitmq.com/getstarted.html).

## Error handling

If an exceptions is thrown by the methods consuming the incoming messages (subscribers) the consumer will stop, unless some error policies are defined.

The built-in policies are:
* <xref:Silverback.Messaging.Inbound.ErrorHandling.StopConsumerErrorPolicy> (default)
* <xref:Silverback.Messaging.Inbound.ErrorHandling.SkipMessageErrorPolicy>
* <xref:Silverback.Messaging.Inbound.ErrorHandling.RetryErrorPolicy>
* <xref:Silverback.Messaging.Inbound.ErrorHandling.MoveMessageErrorPolicy>
* <xref:Silverback.Messaging.Inbound.ErrorHandling.ErrorPolicyChain>

# [Fluent](#tab/error-handling-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events", "inventory-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                            config.AutoOffsetReset = AutoOffsetResetType.Earliest; 
                        })
                    .OnError(policy => policy
                        .Retry(3, TimeSpan.FromSeconds(1))
                        .ThenSkip())));
}
```
# [Legacy](#tab/error-handling-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint(
                    "order-events", 
                    "inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                        AutoOffsetReset = AutoOffsetResetType.Earliest
                    },
                    ErrorPolicy = new ErrorPolicyChain(
                        new RetryErrorPolicy().MaxFailedAttempts(5),
                        new SkipErrorPolicy()) 
                });
}
```
***

> [!Important]
> If the processing still fails after the last policy is applied the exception will be returned to the consumer, causing it to stop.

> [!Important]
> The number of attempts are tracked according to the message id [header](xref:headers). A message id must be provided in order for the `MaxFailedAttempts` mechanism to work. This is ensured by the Silverback producer but might not be the case when consuming messages coming from other sources.
> Some message broker implementations might transparently cope with the missing message id header and derive it from other identifiers (e.g. the kafka message key) but it's not automatically guaranteed that they will always be unique. You should carefully check that before relying on this feature.

> [!Important]
> The <xref:Silverback.Messaging.Inbound.ErrorHandling.RetryErrorPolicy> will prevent the message broker to be polled for the duration of the configured delay, which could lead to a timeout. With Kafka you should for example set the `max.poll.interval.ms` settings to an higher value.

### Apply rules

Use [ApplyTo](xref:Silverback.Messaging.Inbound.ErrorHandling.ErrorPolicyBase#Silverback_Messaging_Inbound_ErrorHandling_ErrorPolicyBase_ApplyTo_Type_) and [Exclude](xref:Silverback.Messaging.Inbound.ErrorHandling.ErrorPolicyBase#Silverback_Messaging_Inbound_ErrorHandling_ErrorPolicyBase_Exclude_Type_) methods to decide which exceptions must be handled by the error policy or take advantage of [ApplyWhen](xref:Silverback.Messaging.Inbound.ErrorHandling.ErrorPolicyBase#Silverback_Messaging_Inbound_ErrorHandling_ErrorPolicyBase_ApplyWhen_Func_IRawInboundEnvelope_Exception_System_Boolean__) to specify a custom apply rule.

```csharp
.OnError(policy => policy
    .MoveToKafkaTopic(
        moveEndpoint => moveEndpoint.ProduceTo("some-other-topic"),
        movePolicy => movePolicy
            .ApplyTo<MyException>()
            .ApplyWhen((msg, ex) => msg.Xy == myValue))
    .ThenSkip());
```
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events", "inventory-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .OnError(policy => policy
                        .MoveToKafkaTopic(
                            moveEndpoint => moveEndpoint.ProduceTo("some-other-topic"),
                            movePolicy => movePolicy
                                .ApplyTo<MyException>()
                                .ApplyWhen((msg, ex) => msg.Xy == myValue))
                        .ThenSkip())));
}
```


### Publishing events

Messages can be published when a policy is applied, in order to execute custom code.

# [EndpointsConfigurator](#tab/eventhandler-configurator)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events", "inventory-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .OnError(policy => policy
                        .Retry(3, TimeSpan.FromSeconds(1))
                        .ThenSkip(skipPolicy => skipPolicy
                            .Publish(msg => new ProcessingFailedEvent(msg))))));
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

In some scenario, when having to deal with huge amounts of messages, processing each one of them on its own isn't the most efficient approach. Batch processing allow to process an arbitrary number of unrelated messages as a single unit of work.

<figure>
	<a href="~/images/diagrams/inbound-batch.png"><img src="~/images/diagrams/inbound-batch.png"></a>
    <figcaption>The messages are processed in batches.</figcaption>
</figure>

Refer to the <xref:Silverback.Messaging.Sequences.Batch.BatchSettings> documentation for details about the configuration.

The batch can be subscribed either as [IEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1), [IAsyncEnumerable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1) or <xref:Silverback.Messaging.Messages.IMessageStreamEnumerable`1>. See also <xref:streaming> for details.

# [EndpointsConfigurator (fluent)](#tab/batch-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("inventory-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .EnableBatchProcessing(100, TimeSpan.FromSeconds(5))));
}
```
# [EndpointsConfigurator (legacy)](#tab/batch-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    Batch = new Messaging.Batch.BatchSettings
                    {
                        Size = 100,
                        MaxWaitTime = TimeSpan.FromSeconds(5)
                    }
                });
}
```
# [Subscriber](#tab/batch-subscriber)
```csharp
public class InventoryService
{
    private DbContext _db;

    public InventoryService(MyDbContext db)
    {
        _db = db;
    }

    public async Task OnUpdateBatchReceived(IAsyncEnumerable<InventoryUpdateEvent> events)
    {
        async foreach (var event in events)
        {
            // Process each message
        }

        // Commit all changes in a single transaction
        await _db.SaveChangesAsync();
    }
}
```
***

## Parallelism

The consumer processes the messages sequentially, this is by design.

The <xref:Silverback.Messaging.Broker.KafkaConsumer> is a bit special and actually processes each assigned partition independently and concurrently.

This feature can be toggled using the [ProcessAllPartitionsTogether](xref:Silverback.Messaging.Configuration.Kafka.IKafkaConsumerEndpointBuilder#Silverback_Messaging_Configuration_Kafka_IKafkaConsumerEndpointBuilder_ProcessAllPartitionsTogether) and [ProcessPartitionsIndependently](xref:Silverback.Messaging.Configuration.Kafka.IKafkaConsumerEndpointBuilder#Silverback_Messaging_Configuration_Kafka_IKafkaConsumerEndpointBuilder_ProcessPartitionsIndependently) methods of the <xref:Silverback.Messaging.Configuration.Kafka.IKafkaConsumerEndpointBuilder> (or the [KafkaConsumerEndpoint.ProcessPartitionsIndependently](xref:Silverback.Messaging.KafkaConsumerEndpoint#Silverback_Messaging_KafkaConsumerEndpoint_ProcessPartitionsIndependently) property), while the [LimitParallelism](xref:Silverback.Messaging.Configuration.Kafka.IKafkaConsumerEndpointBuilder#Silverback_Messaging_Configuration_Kafka_IKafkaConsumerEndpointBuilder_LimitParallelism_System_Int32_) method (or the [KafkaConsumerEndpoint.MaxDegreeOfParallelism](xref:Silverback.Messaging.KafkaConsumerEndpoint#Silverback_Messaging_KafkaConsumerEndpoint_MaxDegreeOfParallelism) property) can be used to limit the number of messages being actually processed concurrently.

## Exactly-once processing

Silverback is able to keep track of the messages that have been consumed in order to guarantee that each message is processed exactly once.

### Offset storage

The <xref:Silverback.Messaging.Inbound.ExactlyOnce.OffsetStoreExactlyOnceStrategy> will store the offset of the latest processed message (of each topic/partition) into a database table.

<figure>
	<a href="~/images/diagrams/inbound-offsetstore.png"><img src="~/images/diagrams/inbound-offsetstore.png"></a>
    <figcaption>The offsets are being stored to prevent the very same message to be consumed twice.</figcaption>
</figure>

> [!Note]
> The [Silverback.Core.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore) package is also required and the `DbContext` must include a `DbSet` of <xref:Silverback.Database.Model.StoredOffset>. See also the <xref:dbcontext>.

# [Startup](#tab/offsetstore-startup)
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
                .AddOffsetStoreDatabaseTable())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
} 
```
# [EndpointsConfigurator (fluent)](#tab/offsetstore-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("inventory-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .EnsureExactlyOnce(strategy => strategy.StoreOffsets())));
}
```
# [EndpointsConfigurator (legacy)](#tab/offsetstore-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    ExactlyOnceStrategy = new OffsetStoreExactlyOnceStrategy()
                });
}
```
***

### Inbound log

The <xref:Silverback.Messaging.Inbound.ExactlyOnce.LogExactlyOnceStrategy> will store the identifiers of all processed messages into a database table.

<figure>
	<a href="~/images/diagrams/inbound-log.png"><img src="~/images/diagrams/inbound-log.png"></a>
    <figcaption>The inbound messages are logged to prevent two messages with the same key to be consumed.</figcaption>
</figure>

> [!Note]
> The [Silverback.Core.EntityFrameworkCore](https://www.nuget.org/packages/Silverback.Core.EntityFrameworkCore) package is also required and the `DbContext` must include a `DbSet` of <xref:Silverback.Database.Model.InboundLogEntry>. See also the <xref:dbcontext>.

# [Startup](#tab/log-startup)
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
                .AddInboundLogDatabaseTable())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
} 
```
# [EndpointsConfigurator (fluent)](#tab/log-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("inventory-events")
                    .Configure(config => 
                        {
                            config.GroupId = "my-consumer";
                        })
                    .EnsureExactlyOnce(strategy => strategy.LogMessages())));
}
```
# [EndpointsConfigurator (legacy)](#tab/log-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer"
                    },
                    ExactlyOnceStrategy = new LogExactlyOnceStrategy()
                });
}
```
***

### Custom store

At the moment only a database accessed using Entity Framework is supported as offset or log storage, but a custom storage can be used implementing <xref:Silverback.Messaging.Inbound.ExactlyOnce.Repositories.IOffsetStore> or <xref:Silverback.Messaging.Inbound.ExactlyOnce.Repositories.IInboundLog>.

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
                .AddOffsetStore<MyCustomOffsetStore>())
            .AddEndpointsConfigurator<MyEndpointsConfigurator>();
    }
} 
```

## Samples

* [All](xref:samples)
