---
uid: broker-callbacks
---

# Broker Callbacks

Broker callbacks let you react to lifecycle events raised by broker clients (Kafka consumers/producers, MQTT clients, etc.).

## Register a Callback Handler

To handle callbacks, implement one (or more) callback interfaces and register your handler as an `IBrokerClientCallback`.

You can register the handler with the desired lifetime:

- `AddSingletonBrokerClientCallback<THandler>()`
- `AddScopedBrokerClientCallback<THandler>()`
- `AddTransientBrokerClientCallback<THandler>()`

## Generic Callback

The following callback applies to all broker implementations:

- <xref:Silverback.Messaging.Broker.Callbacks.IBrokerClientsConfiguredCallback>\
  Invoked immediately after clients and endpoints have been configured.

### Example

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer(...)
        .AddConsumer(...))
    .AddSingletonBrokerClientCallback<MyCallbackHandler>();
```

```csharp
public class MyCallbackHandler : IBrokerClientsConfiguredCallback
{
    public async Task OnBrokerClientsConfiguredAsync()
    {
        // Perform required initialization logic.
    }
}
```

## Kafka-Specific Callbacks

[Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) (used by the Kafka integration) exposes events for partition assignments, errors, statistics, and more. Silverback surfaces those events as callbacks.

### Consumer callbacks

Available callbacks for Kafka consumers:

- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionsAssignedCallback>\
  Triggered when the consumer receives a new partition assignment.
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionsRevokedCallback>\
  Triggered when partition assignments are revoked.
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaOffsetCommittedCallback>\
  Triggered when the consumer commits offsets.
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerErrorCallback>\
  Triggered when an error occurs.
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerStatisticsCallback>\
  Triggered when consumer statistics are received (requires `StatisticsIntervalMs > 0`).
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerLogCallback>\
  Triggered when the consumer logs a message.
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionEofCallback>\
  Triggered when the consumer reaches the end of a partition (requires `EnablePartitionEof = true`).

#### Example: reset offsets on partition assignment

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer(...))
    .AddSingletonBrokerClientCallback<OffsetsResetCallbackHandler>();
```

```csharp
public class OffsetsResetCallbackHandler : IKafkaPartitionsAssignedCallback
{
    public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
        IReadOnlyCollection<TopicPartition> topicPartitions,
        IKafkaConsumer consumer)
    {
        // Reset offset to beginning for each assigned partition.
        return topicPartitions.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning));
    }
}
```

### Producer callbacks

Available callbacks for Kafka producers:

- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaProducerStatisticsCallback>\
  Triggered when producer statistics are received (requires `StatisticsIntervalMs > 0`).
- <xref:Silverback.Messaging.Broker.Callbacks.IKafkaProducerLogCallback>\
  Triggered when the producer logs a message.

## MQTT-Specific Callbacks

MQTT clients expose connect/disconnect lifecycle events.

- <xref:Silverback.Messaging.Broker.Callbacks.IMqttClientConnectedCallback>\
  Triggered when the client successfully connects to the broker.
- <xref:Silverback.Messaging.Broker.Callbacks.IMqttClientDisconnectingCallback>\
  Triggered when the client is about to disconnect (not called if the connection is lost unexpectedly).

#### Example: publish a message when the client connects

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .AddClient(...))
    .AddSingletonBrokerClientCallback<ConnectionCallbackHandler>();
```

```csharp
public class ConnectionCallbackHandler : IMqttClientConnectedCallback
{
    private readonly IPublisher _publisher;

    public ConnectionCallbackHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task OnClientConnectedAsync(MqttClientConfig config) =>
        await _publisher.PublishAsync(new ClientConnectedMessage());
}
```

## Additional Resources

- [API Reference](xref:Silverback)
