---
uid: conversion
---

# Seamless Message Conversion

The messages exchanged with other services are a public contract and should evolve independently of your internal model. A common pattern is therefore to convert the internal messages (e.g., domain events) into dedicated integration messages before they leave the service, or to translate the consumed messages into a different format before forwarding them.

In Silverback, this conversion is just a subscriber: when a subscriber returns one or more messages, these are automatically republished to the message bus and, if their type is mapped to a producer endpoint, produced to the message broker.

## Map Internal Messages to Integration Messages

Subscribe to the internal message and return the integration message.

```csharp
public class OrderEventsMapper
{
    public OrderSubmittedIntegrationEvent Map(OrderSubmittedDomainEvent domainEvent) =>
        new()
        {
            OrderId = domainEvent.Source!.Id,
            CustomerId = domainEvent.Source.CustomerId,
            Total = domainEvent.Source.Total
        };
}
```

Then register the subscriber and configure the integration message to be produced.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<OrderSubmittedIntegrationEvent>("orders", endpoint => endpoint
                .ProduceTo("orders")
                .SetKafkaKey(message => message?.OrderId))))
    .AddSingletonSubscriber<OrderEventsMapper>();
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("client1", client => client
            .WithClientId("order-service")
            .Produce<OrderSubmittedIntegrationEvent>("orders", endpoint => endpoint
                .ProduceTo("orders/submitted")
                .WithAtLeastOnceQoS())))
    .AddSingletonSubscriber<OrderEventsMapper>();
```
***

For simple mappings, a delegate subscriber is enough:

```csharp
services.AddSilverback()
    .AddDelegateSubscriber<OrderSubmittedDomainEvent, OrderSubmittedIntegrationEvent>(domainEvent =>
        new OrderSubmittedIntegrationEvent
        {
            OrderId = domainEvent.Source!.Id
        });
```

A subscriber can also return a collection (`IEnumerable<T>`, `IReadOnlyCollection<T>`, etc.) to split a message into several messages, or `null` / an empty collection to filter it out.

```csharp
public IEnumerable<IIntegrationEvent> Map(OrderSubmittedDomainEvent domainEvent)
{
    yield return new OrderSubmittedIntegrationEvent { OrderId = domainEvent.Source!.Id };

    if (domainEvent.Source.IsFirstOrder)
        yield return new NewCustomerIntegrationEvent { CustomerId = domainEvent.Source.CustomerId };
}
```

> [!Important]
> Only the returned objects implementing <xref:Silverback.Messaging.Messages.IMessage> (e.g., <xref:Silverback.Messaging.Messages.IIntegrationEvent>) are republished. To republish other types, register them (or a base type or interface) via `HandleMessagesOfType<T>()`. See <xref:bus> for details.

## Add Headers and Metadata

The returned messages are wrapped in an envelope with the default settings. Configure the headers or the Kafka key in the endpoint (`AddHeader`, `SetKafkaKey`, etc.) or via attributes on the message model (`[Header("x-my-header")]`, `[KafkaKeyMember]`). See <xref:producing-headers> and <xref:kafka-partitioning>.

If the metadata depends on the source message, inject the <xref:Silverback.Messaging.Publishing.IPublisher> and publish the converted message explicitly instead of returning it:

```csharp
public class OrderEventsMapper
{
    private readonly IPublisher _publisher;

    public OrderEventsMapper(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task MapAsync(OrderSubmittedDomainEvent domainEvent) =>
        _publisher.WrapAndPublishAsync(
            new OrderSubmittedIntegrationEvent { OrderId = domainEvent.Source!.Id },
            envelope => envelope
                .AddHeader("x-tenant", domainEvent.Source.TenantId)
                .SetKafkaKey(domainEvent.Source.CustomerId));
}
```

## Translate Consumed Messages

The same technique works for consumed messages, for example to bridge two topics, two brokers, or two versions of a contract. The returned message is produced as part of the processing of the consumed message.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options
        .AddKafka()
        .AddMqtt())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("telemetry-bridge")
            .Consume<TelemetryV1>("telemetry-v1", endpoint => endpoint
                .ConsumeFrom("telemetry-v1"))))
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("client1", client => client
            .WithClientId("telemetry-bridge")
            .Produce<TelemetryV2>("telemetry-v2", endpoint => endpoint
                .ProduceTo("telemetry/v2")
                .WithAtLeastOnceQoS())))
    .AddDelegateSubscriber<TelemetryV1, TelemetryV2>(message => new TelemetryV2(message.DeviceId, message.Value));
```

## Consume Into Your Own Model

Conversion is often not needed on the consumer side at all: when the endpoint specifies the message type (`Consume<TMessage>`), the JSON deserializer ignores the message type header sent by the producer and deserializes the payload into `TMessage`. Each service can therefore declare its own model for the same contract, containing only the properties it needs. See <xref:deserialization> for the available options.

## Caveats

- **Error handling**: when converting a consumed message, a failure to produce the converted message fails the processing of the consumed message, and the endpoint error policy is applied. Since the processing may be retried, the converted message may be produced more than once.
- **Transactions**: to produce the integration messages only if the database transaction commits, use the <xref:outbox>.
- **Local subscribers**: messages routed to a producer endpoint are not delivered to the local subscribers, unless `EnableSubscribing()` is set on the endpoint.
- **Loops**: make sure that a returned message is not handled by the same subscriber that produced it.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:bus>
- <xref:producing>
- <xref:domain-events>
- <xref:outbox>
- <xref:deserialization>
