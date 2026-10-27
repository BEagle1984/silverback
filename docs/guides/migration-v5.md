---
uid: migration-v5
title: Migrating from v4 to v5
---

# Migrating from v4 to v5

Silverback 5 is a complete refactoring of the library. Most concepts (message bus, subscribers, behaviors, producers and consumers, error policies, outbox) are still there, but the configuration API, the storage layer and several interfaces changed. This guide walks you through the most common steps to upgrade an existing v4 application.

> [!Tip]
> The [release notes](xref:releases-500) contain the complete list of the new features and breaking changes.

## 1. Target .NET 10 and Update the Packages

Starting with v5, Silverback targets only the latest .NET LTS release (currently .NET 10).

- Update your projects to `net10.0`.
- Upgrade all `Silverback.*` packages to the same 5.x version.
- The RabbitMQ integration has been discontinued: [Silverback.Integration.RabbitMQ](https://www.nuget.org/packages/Silverback.Integration.RabbitMQ) is not available for v5.
- The storage-dependent features moved to the new storage packages: [Silverback.Storage.EntityFramework](https://www.nuget.org/packages/Silverback.Storage.EntityFramework/), [Silverback.Storage.PostgreSql](https://www.nuget.org/packages/Silverback.Storage.PostgreSql/), [Silverback.Storage.Sqlite](https://www.nuget.org/packages/Silverback.Storage.Sqlite/) and [Silverback.Storage.Memory](https://www.nuget.org/packages/Silverback.Storage.Memory/) (see <xref:storage>).

Some extension methods moved to different namespaces, so a few `using` directives need to be adjusted (e.g. `Silverback.Messaging.Configuration` → `Silverback.Configuration`).

## 2. Migrate the Endpoints Configuration

The biggest change is the configuration of the broker endpoints. `AddKafkaEndpoints`/`AddMqttEndpoints` with `AddOutbound`/`AddInbound` have been replaced by `AddKafkaClients`/`AddMqttClients`, where you configure the clients (producers and consumers) and their typed endpoints using `Produce<TMessage>` and `Consume<TMessage>`. The Confluent and MQTTnet settings are now exposed as fluent configuration methods instead of the `Configure` callback.

# [v4](#tab/v4)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaEndpoints(endpoints => endpoints
        .Configure(config =>
        {
            config.BootstrapServers = "PLAINTEXT://localhost:9092";
        })
        .AddOutbound<OrderCreatedEvent>(endpoint => endpoint
            .ProduceTo("order-events"))
        .AddInbound(endpoint => endpoint
            .ConsumeFrom("inventory-events")
            .Configure(config =>
            {
                config.GroupId = "my-consumer";
                config.AutoOffsetReset = AutoOffsetResetType.Earliest;
            })
            .OnError(policy => policy.Retry(3).ThenSkip())));
```
# [v5](#tab/v5)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(producer => producer
            .Produce<OrderCreatedEvent>(endpoint => endpoint
                .ProduceTo("order-events")))
        .AddConsumer(consumer => consumer
            .WithGroupId("my-consumer")
            .AutoResetOffsetToEarliest()
            .Consume<InventoryEvent>(endpoint => endpoint
                .ConsumeFrom("inventory-events")
                .OnError(policy => policy.Retry(3).ThenSkip()))));
```
***

Things to keep in mind:

- In Kafka, each `AddProducer`/`AddConsumer` creates a dedicated client. Group multiple endpoints in the same client to reduce the number of connections. With MQTT, a single client (`AddClient`) can both produce and consume.
- The message type passed to `Consume<TMessage>` is used to select the default deserializer, so the `x-message-type` header is not needed anymore for non-polymorphic topics (see <xref:deserialization>).
- Some settings moved from the endpoint to the consumer, such as the parallelism (`LimitParallelism`, `ProcessAllPartitionsTogether`, `LimitBackpressure`).
- `IEndpointsConfigurator` has been replaced by <xref:Silverback.Messaging.Configuration.IBrokerClientsConfigurator>, registered via `AddBrokerClientsConfigurator`.
- Assigning a name to the clients and endpoints (e.g. `AddProducer("producer1", ...)`, `Produce<T>("endpoint1", ...)`) is optional but recommended, and required when using the outbox.

See <xref:broker>, <xref:producing> and <xref:consuming> for the details.

## 3. Replace the Specialized Publishers

`IEventPublisher`, `ICommandPublisher` and `IQueryPublisher` have been removed (as well as the `UseModel` configuration method). Use the equivalent extension methods on <xref:Silverback.Messaging.Publishing.IPublisher> (from the `Silverback.Core.Model` package) or inject the <xref:Silverback.Messaging.Publishing.IApplicationPublisher> if you prefer a more easily mockable interface.

# [v4](#tab/v4)
```csharp
public class OrderService
{
    private readonly IEventPublisher _publisher;

    public OrderService(IEventPublisher publisher) => _publisher = publisher;

    public Task PlaceOrderAsync(Order order) =>
        _publisher.PublishAsync(new OrderCreatedEvent(order.Id));
}
```
# [v5](#tab/v5)
```csharp
public class OrderService
{
    private readonly IPublisher _publisher;

    public OrderService(IPublisher publisher) => _publisher = publisher;

    public Task PlaceOrderAsync(Order order) =>
        _publisher.PublishEventAsync(new OrderCreatedEvent(order.Id));
}
```
***

The <xref:Silverback.Messaging.Publishing.IPublisher> is now registered as transient and can be injected into singleton services. All publishing methods accept a `CancellationToken`.

## 4. Review the Behaviors and Callbacks

- <xref:Silverback.Messaging.Publishing.IBehavior>, <xref:Silverback.Messaging.Broker.Behaviors.IProducerBehavior> and <xref:Silverback.Messaging.Broker.Behaviors.IConsumerBehavior> now return `ValueTask` and receive a `CancellationToken` (see <xref:bus>, <xref:producer-behaviors> and <xref:consumer-behaviors>).
- `IBrokerCallback` and the `Add*BrokerCallbackHandler` methods have been renamed to `IBrokerClientCallback` and `Add*BrokerClientCallback` (see <xref:broker-callbacks>).
- `IBroker` and `IBrokerCollection` have been removed. Use `IProducerCollection`, `IConsumerCollection` and <xref:Silverback.Messaging.Broker.IBrokerClientCollection> instead (see <xref:broker>).
- The `IMessageSerializer` interface has been split into <xref:Silverback.Messaging.Serialization.IMessageSerializer> and <xref:Silverback.Messaging.Serialization.IMessageDeserializer>.

## 5. Migrate the Outbox and the Storage

The outbox and all storage-backed features have been rewritten from scratch.

- Reference the storage package matching your database and configure the outbox and the outbox worker as shown in <xref:outbox>.
- With Entity Framework, add the `SilverbackOutboxMessage` (and, if used, the `SilverbackLock`) entities to your `DbContext` and create a migration. The table schema differs from v4.
- Writing to the outbox is not automatically part of your database transaction anymore: begin the transaction explicitly and enlist it in the publisher (`EnlistDbTransaction`) to keep the application changes and the outbox messages atomic.
- The exactly-once guard (inbound log / offset store) has been replaced by the client-side offset storage (see <xref:kafka-offset>).

> [!Important]
> Make sure the v4 outbox is empty before switching to v5, since the messages stored by v4 cannot be processed by the new outbox worker.

## 6. Check the Changed Defaults

A few defaults changed and might affect the behavior of your application:

- The Kafka key isn't set to a random `Guid` anymore but is `null` unless explicitly set (see <xref:kafka-partitioning>).
- The JSON deserializers don't use the `x-message-type` header by default. Enable it explicitly if you rely on polymorphic deserialization (see <xref:deserialization>).
- The Newtonsoft.Json serializer uses `TypeNameHandling.None` and the Newtonsoft.Json default value handling.
- The Kafka consumer processes up to 100 partitions in parallel (`MaxDegreeOfParallelism`) and buffers up to 50 messages per partition (`BackpressureLimit`).
- The MQTT producer doesn't throw anymore if no subscriber is consuming the produced message.

## 7. Update the Tests

The testing helpers (<xref:Silverback.Testing.ITestingHelper>, <xref:Silverback.Testing.IKafkaTestingHelper> and <xref:Silverback.Testing.IMqttTestingHelper>) changed in a few places, and new envelope builders are available to create test envelopes. See <xref:testing>.

## Additional Resources

- [Release Notes v5](xref:releases-500)
- <xref:setup>
- <xref:broker>
- <xref:samples>
- [v4 documentation](/v4/)
