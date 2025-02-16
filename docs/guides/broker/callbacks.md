---
uid: broker-callbacks
---

# Broker Callbacks

Broker callbacks allow you to handle specific events during the lifecycle of a message broker client. These callbacks help you react to various broker-related events.

## Registering a Callback Handler

To use a callback, implement the corresponding interface and register the handler using one of the following methods:
* `AddSingletonBrokerCallbacksHandler<THandler>()`
* `AddScopedBrokerCallbacksHandler<THandler>()`
* `AddTransientBrokerCallbacksHandler<THandler>()`

## Generic Callback

The following callback applies to all broker implementations:
* <xref:Silverback.Messaging.Broker.Callbacks.IBrokerClientsConfiguredCallback>\
  Invoked immediately after the broker clients have been initialized based on the specified client and endpoint configuration.

### Example: Registering a IBrokerClientsConfiguredCallback

```csharp
.AddSilverback()
.WithConnectionToMessageBroker(options => options.AddKafka())
.AddKafkaClients(clients => clients
    .WithBootstrapServers(...)
    .AddProducer(...)
    .AddConsumer(...))
.AddSingletonBrokerCallbacksHandler<MyCallbackHandler>();
```

```csharp
public class MyCallbackHandler : IBrokerClientsConfiguredCallback
{
    public async Task OnBrokerClientsConfiguredAsync()
    {
        // Perform required initialization logic
    } 
}
```

## Kafka-Specific Callbacks

[Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) the underlying library used for Kafka integration, provides various events for handling partition assignments, logging, statistics, and more. Silverback proxies these events as callbacks.

### Consumer Callbacks

The following callbacks are available for Kafka consumers:
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionsAssignedCallback>\
  Triggered when the consumer receives a new partition assignment.
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionsRevokedCallback>\
  Triggered when partition assignments are revoked.
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaOffsetCommittedCallback>\
  Triggered when the consumer commits offsets.
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerErrorCallback>\
  Triggered when an error occurs.
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerStatisticsCallback>\
  Triggered when consumer statistics are received (requires `StatisticsIntervalMs > 0`).
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerLogCallback>\
  Triggered when the consumer logs a message.
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionEofCallback>\
  Triggered when the consumer reaches the end of a partition (requires EnablePartitionEof = true).

#### Example: Resetting Offsets on Partition Assignment

In this example, the callback resets the offset to the beginning of each assigned partition, ensuring that past messages are replayed.

```csharp
.AddSilverback()
.WithConnectionToMessageBroker(options => options.AddKafka())
.AddKafkaClients(clients => clients
    .WithBootstrapServers(...)
    .AddProducer(...)
    .AddConsumer(...))
.AddSingletonBrokerCallbackHandler<OffsetsResetCallbackHandler>();
```

```csharp
public class OffsetsResetCallbackHandler : IKafkaPartitionsAssignedCallback
{
    public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
        IReadOnlyCollection<TopicPartition> topicPartitions,
        IKafkaConsumer consumer)
    {
        // Reset offset to beginning for each assigned partition
        return topicPartitions.Select(topicPartition => 
            new TopicPartitionOffset(topicPartition, Offset.Beginning));
    }
}
```

### Producer Callbacks

The following callbacks are available for Kafka producers:
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaProducerStatisticsCallback>\
  Triggered when producer statistics are received (requires `StatisticsIntervalMs > 0`).
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaProducerLogCallback>\
  Triggered when the producer logs a message.

## MQTT-Specific Callbacks

MQTT clients trigger some lifecycle events that can be handled using the following callbacks:
* <xref:Silverback.Messaging.Broker.Callbacks.IMqttClientConnectedCallback>\
  Triggered when the client successfully connects to the broker.
* <xref:Silverback.Messaging.Broker.Callbacks.IMqttClientDisconnectingCallback>\
  Triggered when the client is about to disconnect (not called if the connection is lost unexpectedly).

#### Example: Sending a Message When the Client Connects

In this example, a message is published as soon as the MQTT client establishes a connection.

```csharp
services
    .AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .AddClient(...))
    .AddSingletonBrokerCallbacksHandler<ConnectionCallbackHandler>();
```

```csharp
public class ConnectionCallbackHandler : IMqttClientConnectedCallback
{
    private readonly IPublisher _publisher;
    
    public ConnectionCallbackHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }
            
    public Task OnClientConnectedAsync(MqttClientConfig config) =>
        _publisher.PublishAsync(new ClientConnectedMessage());
}
```

## Additional Resources

For more details, refer to the [API Reference](~/api/Silverback.html) for each callback interface.
