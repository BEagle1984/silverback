---
uid: multiple-consumers
---

# Multiple Consumers for the Same Topic

Sometimes the same application needs to consume the same topic more than once, independently. For example, one consumer updates a read model while another one forwards the events to an external system, and each of them must track its own progress and apply its own error handling.

A single consumer (Kafka) or client (MQTT) cannot subscribe to the same topic twice: Silverback rejects the configuration if two endpoints of the same consumer/client point to the same topic. You therefore need to configure separate consumers/clients and then route the consumed messages to the right subscriber.

## Configure the Consumers

# [Kafka](#tab/kafka)
With Kafka, each consumer must use a different consumer group. Each group receives all messages and commits its own offsets.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("read-model-consumer", consumer => consumer
            .WithGroupId("read-model")
            .Consume<OrderEvent>("orders-read-model", endpoint => endpoint
                .ConsumeFrom("orders")))
        .AddConsumer("export-consumer", consumer => consumer
            .WithGroupId("export")
            .Consume<OrderEvent>("orders-export", endpoint => endpoint
                .ConsumeFrom("orders")
                .OnError(policy => policy.Retry(5).ThenSkip()))));
```
# [MQTT](#tab/mqtt)
With MQTT, each client must use a different client ID. The broker delivers a copy of each message to every client.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("read-model-client", client => client
            .WithClientId("my-app-read-model")
            .Consume<OrderEvent>("orders-read-model", endpoint => endpoint
                .ConsumeFrom("orders")
                .WithAtLeastOnceQoS()))
        .AddClient("export-client", client => client
            .WithClientId("my-app-export")
            .Consume<OrderEvent>("orders-export", endpoint => endpoint
                .ConsumeFrom("orders")
                .WithAtLeastOnceQoS()
                .OnError(policy => policy.Retry(5).ThenSkip()))));
```
***

> [!Important]
> Use different consumer/client names and endpoint names. Configuration actions registered with the same consumer/client name are merged into a single consumer, and using the same endpoint name in two different consumers throws an exception.

## Route the Messages to the Subscribers

All consumed messages are published to the same message bus. Without any filter, a subscriber for `OrderEvent` would receive each message twice: once per consumer. Use a filter attribute to restrict each subscriber method to a specific consumer:

- <xref:Silverback.Messaging.Subscribers.ConsumerNameFilterAttribute> filters by consumer/client name (the name passed to `AddConsumer` or `AddClient`).
- <xref:Silverback.Messaging.Subscribers.KafkaGroupIdFilterAttribute> filters by Kafka consumer group ID.
- <xref:Silverback.Messaging.Subscribers.MqttClientIdFilterAttribute> filters by MQTT client ID.

```csharp
public class OrderSubscriber
{
    [KafkaGroupIdFilter("read-model")]
    public async Task UpdateReadModelAsync(OrderEvent message)
    {
        // Update the read model
    }

    [KafkaGroupIdFilter("export")]
    public async Task ExportAsync(OrderEvent message)
    {
        // Forward the event to the external system
    }
}
```

The filters can also be specified when registering the subscriber, via <xref:Silverback.Messaging.Subscribers.Subscriptions.TypeSubscriptionOptions> or <xref:Silverback.Messaging.Subscribers.Subscriptions.DelegateSubscriptionOptions>:

```csharp
services.AddSilverback()
    .AddScopedSubscriber<ReadModelUpdater>(
        new TypeSubscriptionOptions
        {
            Filters = [new ConsumerNameFilterAttribute("read-model-consumer")]
        })
    .AddScopedSubscriber<OrderExporter>(
        new TypeSubscriptionOptions
        {
            Filters = [new ConsumerNameFilterAttribute("export-consumer")]
        });
```

Alternatively, subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> and inspect the `Consumer` or `Endpoint` properties.

## Scaling Out vs. Consuming Twice

Don't confuse the scenario above with load balancing:

- **Kafka**: consumers sharing the same group ID split the partitions among them. Use the same group ID across your application instances to scale out, and different group IDs to receive every message in each consumer.
- **MQTT**: to load balance across several clients, use a shared subscription (e.g., `ConsumeFrom("$share/my-group/orders")`). The broker delivers each message to only one client of the group.

> [!Tip]
> With MQTT, the client ID must also be unique across your application instances. When scaling out, derive it from something unique per instance (e.g., the machine name), otherwise the clients will keep disconnecting each other.

## Caveats

- Each consumer has its own connection, its own buffers and, in Kafka, its own offsets: a failure in one consumer doesn't affect the others, but the resource usage grows accordingly.
- Each consumer processes the message independently. If both write to the same database, design the handlers to be idempotent and avoid conflicting updates.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming>
- <xref:kafka-subscription>
- <xref:bus>
- <xref:same-message>
