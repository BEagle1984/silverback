---
uid: producing
---

# Producing Messages

The integration with message brokers is based on the concept of producers and consumers. Producers are responsible for sending messages to the broker, while consumers receive and process them. This guide focuses on the producing side.

## Producer Configuration

Silverback provides a fluent API to configure the producer settings. The following example demonstrates how to set up a basic Kafka and MQTT producer.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("messages/my")
                .WithAtLeastOnceQoS());
```
***

The `AddProducer` or `AddClient` method is used to configure the producer. The `Produce<TMessage>` method is used to wire up the producer for a specific message type. The `ProduceTo` method specifies the topic to which the messages should be sent.

Each `AddProducer` call will result in a producer to be created. The `Produce<TMessage>` method can be called multiple times to configure multiple message types for the same producer, each with its own settings, and leading to optimized resource usage and sometimes overall performance in comparison to using a dedicated producer for each message type.

> [!Note]
> While Kafka producers and consumers are different entities, MQTT clients are used for both producing and consuming messages.

> [!Tip]
> Assigning a name to the producer or client and its endpoints is optional but can be useful for logging and debugging purposes, as well as for direct access to the producer or client instance for advanced scenarios. Furthermore, it allows you to ensure that each client and endpoint is only configured once, even if you duplicate the declaration (for example if needed in multiple feature slices).

> [!Tip]
> For a more in-depth documentation about the configuration of the underlying libraries refer to the [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html) and [MQTTNet documentation]([MQTTNet documentation](https://github.com/chkr1011/MQTTnet/wiki) respectively.

## Producing Messages

Once the producer is configured, you can use the `IPublisher` to send messages through the message bus and Silverback will take care of routing them to the appropriate producer.

```csharp
await _publisher.PublishAsync(new MyMessage { ... });
```

### WrapAndPublish

The `WrapAndPublish` and `WrapAndPublishAsync` methods can be used to wrap the message in an envelope and add additional metadata, such as headers.

```csharp
await _publisher.WrapAndPublishAsync(
    new MyMessage { ... },
    envelope => envelope
        .AddHeader("x-priority", 1))
        .AddHeader("x-random", Random.Shared.Next()));
```

### WrapAndPublishBatch

The `WrapAndPublishBatch` and `WrapAndPublishBatchAsync` methods can be used to publish multiple messages in a single batch, leveraging the batching capabilities of Kafka for a much greater throughput (see [benchmarks](xref:performance)).

```csharp
public async Task PublishBatch(IEnumerable<MyMessage> messages)
{
    await _publisher.WrapAndPublishBatchAsync(
        messages,
        envelope => envelope
            .AddHeader("x-priority", 1))
            .AddHeader("x-random", Random.Shared.Next()));
}
```

Some overloads allow you to specify a mapping function, so that you can use a streaming source and build the message models on-the-fly before they are wrapped.

```csharp
public async Task PublishBatch(IAsyncEnumerable<Order> orderEntities)
{
    await _publisher.WrapAndPublishBatchAsync(
        orderEntities,
        orderEntity => new OrderReceived { OrderNumer = orderEntity.Number },
        (envelope, orderEntity) => envelope
            .AddHeader("x-priority", orderEntity.Priority)));
}
```

## Routing

The `Produce<TMessage>` method is used to configure the specified message type to be routed through the producer. The `ProduceTo` method specifies the topic to which the messages should be sent. Each endpoint can be configured with additional settings, such as specific headers or message properties, or a different serialization strategy.

<figure>
	<a href="~/images/diagrams/outbound-routing.png"><img src="~/images/diagrams/outbound-routing.png"></a>
    <figcaption>The messages are dynamically routed to the appropriate endpoint.</figcaption>
</figure>

By default, Silverback routes the messages based on the message type. However, you can also implement custom routing logic.

> [!Note]  
> Messages published and routed to a producer cannot be subscribed to locally (within the same process) unless the endpoint is explicitly configured with the `EnableSubscribing` method. However, you can still subscribe to the <xref:Silverback.Messaging.Messages.IOutboundEnvelope`1> without `EnableSubscribing`.

### Routing Function

A routing function can be used to determine the endpoint based on the message content or metadata (via <xref:Silverback.Messaging.Messages.IOutboundEnvelope`1>). The function is called for each message and should return the destination topic name.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers(...)
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo(envelope => envelope.Headers.GetValue<int>("x-random") % 2 == 0 ? "my-even" : "my-odd"))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp(...)
        .AddClient(client => client
            .WithClientId("my.client")
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo(envelope => envelope.Headers.GetValue<int>("x-random") % 2 == 0 ? "my/even" : "my/odd")));
```
***

> [!Tip]
> Several overloads of the `ProduceTo` method allow you to specify the routing function based on the bare message or the envelope, giving you access to the message content, its headers and other metadata.
> Furthermore, for Kafka you can also specify the partition to produce to.

You can also use a built-in string format to specify only the variable parts of the topic name.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers(...)
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo(
                    "my-{0}",
                    message => message.Id % 2 == 0 ? ["even"] : ["odd"]))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp(...)
        .AddClient(client => client
            .WithClientId("my.client")
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo(
                    "my/{0}",
                    message => message.Id % 2 == 0 ? ["even"] : ["odd"]))));
```
***

### Resolver Class

The custom routing logic can also be implemented in a dedicated endpoint resolver class. The resolver must implement the <xref:Silverback.Messaging.Producing.EndpointResolvers.IKafkaProducerEndpointResolver`1> or <xref:Silverback.Messaging.Producing.EndpointResolvers.IMqttProducerEndpointResolver`1> interface and is resolved via the DI container, thus allowing you to inject dependencies.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers(...)
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .UseEndpointResolver<MyTopicResolver>())));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp(...)
        .AddClient(client => client
            .WithClientId("my.client")
            .Produce<MyMessage>(endpoint => endpoint
                .UseEndpointResolver<MyTopicResolver>())));
```
***

# [Kafka](#tab/kafka)
```csharp
public class MyTopicResolver : IKafkaProducerEndpointResolver<MyMessage>
{
    public TopicPartition GetTopicPartition(IOutboundEnvelope<MyMessage> envelope)
    {
        return new TopicPartition(
            "my-" + envelope.Message.Priority,
            envelope.Headers.GetValue<int>("x-random") % 10);
    }
}
```
# [MQTT](#tab/mqtt)
```csharp
public class MyTopicResolver : IMqttProducerEndpointResolver<MyMessage>
{
    public string GetTopic(IOutboundEnvelope<MyMessage> envelope)
    {
        return "my/" + envelope.Message.Priority;
    }
}
```
***

### Dynamic Routing

The routing can also be fully dynamic and be delegated to the actual code publishing the message.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers(...)
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceToDynamicTopic())));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp(...)
        .AddClient(client => client
            .WithClientId("my.client")
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceToDynamicTopic())));
```
***

# [Kafka](#tab/kafka)
```csharp
await _publisher.WrapAndPublishAsync(
    new MyMessage { ... },
    envelope => envelope
        .SetKafkaDestinationTopic("my-topic")));
```
# [MQTT](#tab/mqtt)
```csharp
await _publisher.WrapAndPublishAsync(
    new MyMessage { ... },
    envelope => envelope
        .SetMqttDestinationTopic("my/topic")));
```
***

### Kafka Partitioning

For Kafka producers, you can also specify the partition to produce to or influence it using a partitioning key. More information can be found in the <xref:kafka-partitioning> guide.

### Filtering

An additional function can be used to filter the messages that should be produced to the specific endpoint. The function is called for each message and should return `true` if the message should be produced.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers(...)
        .AddProducer(producer => producer
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo("my-even-messages")
                .Filter(message => message.Number % 2 == 0))
            .Produce<MyMessage>(endpoint => endpoint
                .ProduceTo("my-odd-messages")
                .Filter(message => message.Number % 2 == 1))));
```

## Additional Resources

For more information about the producer configuration possibilities refer to the other guides in this section and the [API Reference](xref:Silverback).
