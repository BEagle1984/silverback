---
uid: consuming
---

# Consuming Messages

A consumer reads messages from the broker and dispatches them to your subscribers via the Silverback message bus.

This guide covers the basics: configuring consumers, handling messages, parallelism, and error handling.

## Configure a Consumer

A *consumer* (Kafka) or *client* (MQTT) can consume one or more *endpoints*. Each endpoint defines the message type and the broker address to consume from (topic for Kafka, topic filter for MQTT).

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("messages/my")
                .WithAtLeastOnceQoS())));
```
***

> [!Tip]
> **Configuration Notes**
> 
> - `AddConsumer` (Kafka) / `AddClient` (MQTT) creates a broker connection with its own settings.
> - Call `Consume<TMessage>` multiple times to add multiple endpoints to the same consumer/client. This usually reduces resource usage compared to creating one consumer per endpoint.
> - In Kafka, producers and consumers are separate entities. In MQTT, the same client can both produce and consume.
> - Naming consumers/clients and endpoints is optional but recommended for logging and diagnostics. It can also prevent double-registration if the same configuration block runs more than once.
> - For broker-specific options, see the upstream client docs: [confluent-kafka-dotnet](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html) and [MQTTnet](https://github.com/chkr1011/MQTTnet/wiki).

## Handle Messages

Consumed messages are published to the <xref:bus> and handled by your subscribers.

You can subscribe to:

- The deserialized message (`MyMessage`).
- The envelope (`IInboundEnvelope<MyMessage>`) to access metadata (headers, broker identifier, etc.).

```csharp
public class MySubscriber
{
    public async Task HandleAsync(MyMessage message)
    {
        Console.WriteLine($"Received message: {message.Content}");
    }

    public async Task HandleAsync(IInboundEnvelope<MyMessage> envelope)
    {
        Console.WriteLine($"Received message: {envelope.Message.Content}");

        // Kafka-only metadata example.
        Console.WriteLine($"Kafka key: {envelope.GetKafkaKey()}");
    }
}
```

Subscriber methods can be synchronous or asynchronous (`Task`/`ValueTask`). A message is considered successfully processed when all matching subscribers complete without throwing.

> [!Warning]
> The same message can be handled by multiple subscribers. If you do that, make sure handlers are idempotent: if any subscriber fails, the message is considered failed and the configured error policy is applied.

For higher throughput, consider <xref:batch-processing>.

## Parallelism

Kafka and MQTT have different parallelism models:

- **Kafka**: parallelism is enabled by default. Messages from different partitions can be processed concurrently; messages within the same partition are always processed sequentially.
- **MQTT**: processing is sequential by default; you can enable parallel processing.

### Kafka

Limit the maximum degree of parallelism:

```csharp
.AddConsumer("consumer1", consumer => consumer
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .LimitParallelism(3)
        .ConsumeFrom("my-topic-1")
        .ConsumeFrom("my-topic-2")))
```

Process all partitions in a single stream (disables partition-based parallelism):

```csharp
.AddConsumer("consumer1", consumer => consumer
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ProcessAllPartitionsTogether()
        .ConsumeFrom("my-topic-1")
        .ConsumeFrom("my-topic-2")))
```

### MQTT

Enable parallel processing and set the maximum degree of parallelism:

```csharp
.AddClient("my-client", client => client
    .WithClientId("client1")
    .EnableParallelProcessing(10)
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("messages/topic1")
        .ConsumeFrom("messages/topic2")
        .WithAtLeastOnceQoS()));
```

## Error Handling

If a subscriber throws, Silverback applies the endpoint error policy. The default policy stops the consumer.

Built-in policies:

- <xref:Silverback.Messaging.Consuming.ErrorHandling.StopConsumerErrorPolicy> (default)
- <xref:Silverback.Messaging.Consuming.ErrorHandling.SkipMessageErrorPolicy>
- <xref:Silverback.Messaging.Consuming.ErrorHandling.RetryErrorPolicy>
- <xref:Silverback.Messaging.Consuming.ErrorHandling.MoveMessageErrorPolicy>
- <xref:Silverback.Messaging.Consuming.ErrorHandling.ErrorPolicyChain>

Policies can be chained:

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic")
                .OnError(policy => policy.Retry(2).ThenSkip()))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("messages/my")
                .OnError(policy => policy.Retry(2).ThenSkip())
                .WithAtLeastOnceQoS())));
```
***

> [!Important]
> If processing still fails after the last policy in the chain, the exception is returned to the consumer and the consumer stops.

> [!Important]
> <xref:Silverback.Messaging.Consuming.ErrorHandling.RetryErrorPolicy> waits before retrying and may delay polling. With Kafka, ensure broker/client timeouts (for example `max.poll.interval.ms`) are configured accordingly.

### Apply rules

Use `ApplyTo`, `Exclude`, and `ApplyWhen` to control which exceptions are handled and when.

```csharp
.OnError(policy => policy
    .MoveToKafkaTopic(
        moveEndpoint => moveEndpoint.ProduceTo("some-other-topic"),
        movePolicy => movePolicy
            .ApplyTo<MyException>()
            .ApplyWhen((msg, ex) => msg.Xy == myValue))
    .ThenSkip());
```

### Publish events

You can publish a message when a policy is applied (for logging, alerts, etc.).

```csharp
.OnError(policy => policy
    .Retry(3, TimeSpan.FromSeconds(1))
    .ThenSkip(skipPolicy => skipPolicy
        .Publish(msg => new ProcessingFailedEvent(msg))));
```

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:bus>
- <xref:batch-processing>
- <xref:samples>
- Other guides in this section for broker-specific and advanced scenarios
