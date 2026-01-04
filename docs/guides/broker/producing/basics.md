---
uid: producing
---

# Producing Messages

This guide explains how to configure producers and publish messages to Kafka or MQTT.

## Configure Producers

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
                .WithAtLeastOnceQoS())));
```
***

Use `AddProducer` (Kafka) or `AddClient` (MQTT) to create a producer/client instance. Use `Produce<TMessage>` to associate message types to endpoints. An endpoint usually specifies a destination topic via `ProduceTo`.

> [!Note]
> With MQTT, the same client instance can both produce and consume.

> [!Tip]
> Naming producers/clients and endpoints is optional, but helps with logs and avoids duplicate registration across modules/features.

> [!Tip]
> Underlying client configuration:
> - Kafka: https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html
> - MQTTnet: https://github.com/chkr1011/MQTTnet/wiki

## Publish Messages

Once configured, publish messages via <xref:Silverback.Messaging.Publishing.IPublisher>.

```csharp
await _publisher.PublishAsync(new MyMessage { /* ... */ });
```

### Add Metadata (WrapAndPublish)

Use `WrapAndPublishAsync` to attach headers and other metadata.

```csharp
await _publisher.WrapAndPublishAsync(
    new MyMessage { /* ... */ },
    envelope => envelope
        .AddHeader("x-priority", 1)
        .AddHeader("x-random", Random.Shared.Next()));
```

### Publish Batches (WrapAndPublishBatch)

Use `WrapAndPublishBatchAsync` to publish multiple messages efficiently.

```csharp
public Task PublishBatchAsync(IEnumerable<MyMessage> messages) =>
    _publisher.WrapAndPublishBatchAsync(
        messages,
        envelope => envelope
            .AddHeader("x-priority", 1)
            .AddHeader("x-random", Random.Shared.Next()));
```

Some overloads allow mapping from a streaming source.

```csharp
public Task PublishBatchAsync(IAsyncEnumerable<Order> orders) =>
    _publisher.WrapAndPublishBatchAsync(
        orders,
        order => new OrderReceived { OrderNumber = order.Number },
        (envelope, order) => envelope.AddHeader("x-priority", order.Priority));
```

## Routing

The `Produce<TMessage>` method is used to configure the specified message type to be routed through the producer. The `ProduceTo` method specifies the topic to which the messages should be sent. Each endpoint can be configured with additional settings, such as specific headers or message properties, or a different serialization strategy.

<figure>
	<a href="~/images/diagrams/outbound-routing.png"><img src="~/images/diagrams/outbound-routing.png" alt="Outbound routing diagram"/></a>
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
                .ProduceTo(envelope => envelope.Headers.GetValue<int>("x-random") % 2 == 0 ? "my/even" : "my/odd"))));
```
***

> [!Tip]
> Several overloads of the `ProduceTo` method let you build the destination topic from the message or the envelope, giving you access to the payload, headers, and broker-specific metadata.

You can also use a format string where only the variable parts are generated.

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
                    message => message.Id % 2 == 0 ? new object[] { "even" } : new object[] { "odd" }))));
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
                    message => message.Id % 2 == 0 ? new object[] { "even" } : new object[] { "odd" }))));
```
***

### Resolver Class

You can also implement routing in a dedicated resolver, resolved via DI (so you can inject services). The resolver must implement:

- <xref:Silverback.Messaging.Producing.EndpointResolvers.IKafkaProducerEndpointResolver`1> (Kafka)
- <xref:Silverback.Messaging.Producing.EndpointResolvers.IMqttProducerEndpointResolver`1> (MQTT)

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

If you want to select the destination at publish time (instead of in configuration), enable dynamic routing.

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
    envelope => envelope.SetKafkaDestinationTopic("my-topic"));
```
# [MQTT](#tab/mqtt)
```csharp
await _publisher.WrapAndPublishAsync(
    new MyMessage { ... },
    envelope => envelope.SetMqttDestinationTopic("my/topic"));
```
***

### Kafka Partitioning

For Kafka producers, you can also specify the partition to produce to or influence it using a partitioning key. See <xref:kafka-partitioning>.

### Filtering

You can add a predicate to filter which messages are produced to a given endpoint.

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

- [API Reference](xref:Silverback)
- <xref:bus>
- <xref:consuming>
- <xref:samples>
- Other guides in this section for in-depth information about producer features.
