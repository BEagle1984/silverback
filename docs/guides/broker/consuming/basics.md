---
uid: consuming
description: "Configure Kafka and MQTT consumers in .NET, handle consumed messages in subscribers, tune parallelism and apply error handling rules."
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
> - For broker-specific options, see the upstream client docs: [confluent-kafka-dotnet](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html) and [MQTTnet](https://github.com/dotnet/MQTTnet/wiki).

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

Subscriber methods can be synchronous or asynchronous (`Task`/`ValueTask`). A message is considered successfully processed when all matching subscribers complete without throwing, and is then committed (Kafka) or acknowledged (MQTT).

> [!Warning]
> The same message can be handled by multiple subscribers, but they share a single outcome. If any subscriber throws, the error policy is applied to the message as a whole:
>
> - **Stop** (default): the message isn't committed, so all the subscribers process it again, including those that already completed successfully, once the consumer is restarted (with MQTT, only with QoS 1 or 2 and a persistent session).
> - **Retry**: all the subscribers are invoked again, including those that already completed successfully.
> - **Skip** or **Move**: the failed message is committed (or acknowledged) and isn't redelivered. By default the subscribers are invoked one after the other, so those following the failing subscriber weren't invoked at all. If the message is moved to a topic that is consumed again (for example the same topic, to retry it later), all the subscribers process it again, including those that already completed successfully.
>
> Prefer a single subscriber per consumed message type and, if several actions are needed, invoke them explicitly from that subscriber. In any case, make sure the handlers are idempotent.
>
> Batch processing has further limitations, see [Multiple Subscribers](xref:batch-processing#multiple-subscribers).

For higher throughput, consider <xref:batch-processing>.

## Parallelism

Kafka and MQTT have different parallelism models:

- **Kafka**: parallelism is enabled by default. Messages from different partitions can be processed concurrently; messages within the same partition are always processed sequentially.
- **MQTT**: processing is sequential by default; you can enable parallel processing.

### Kafka

Limit the maximum degree of parallelism:

```csharp
.AddConsumer("consumer1", consumer => consumer
    .LimitParallelism(3)
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("my-topic-1", "my-topic-2")))
```

Process all partitions in a single stream (disables partition-based parallelism):

```csharp
.AddConsumer("consumer1", consumer => consumer
    .ProcessAllPartitionsTogether()
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("my-topic-1", "my-topic-2")))
```

Both settings apply to the whole consumer, not to a single endpoint. Use `LimitBackpressure(n)` to set the buffer capacity per partition when processing partitions independently. With `ProcessAllPartitionsTogether()`, it sets the capacity of the shared buffer for all partitions. The default limit is 50.

With `ProcessAllPartitionsTogether()`, all the endpoints of the consumer share the same batches, so they must use the same batch settings, and a streaming endpoint must be the only endpoint of the consumer. A batch can then contain the messages from different endpoints: handle it with a single subscriber of a common message type (see [Multiple Subscribers](xref:batch-processing#multiple-subscribers)) and use the same error policy for all the endpoints, since the policy of the endpoint of the first message of the batch applies to the whole batch. Use separate consumers for the endpoints that need different settings.

### MQTT

Enable parallel processing and set the maximum degree of parallelism:

```csharp
.AddClient("my-client", client => client
    .WithClientId("client1")
    .EnableParallelProcessing(10)
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ConsumeFrom("messages/topic1", "messages/topic2")
        .WithAtLeastOnceQoS()));
```

Use `LimitBackpressure(n)` to limit how many incoming MQTT messages Silverback buffers per parallel execution. The number of parallel executions is controlled by `MaxDegreeOfParallelism`, configured with `EnableParallelProcessing(...)`. The total buffer capacity per client is `n × MaxDegreeOfParallelism`, excluding messages already being processed. The default backpressure limit is 2.

For example, `EnableParallelProcessing(4)` with `LimitBackpressure(7)` allows up to 28 buffered messages, plus four being processed. This setting controls Silverback's processing buffers, not buffers in MQTTnet or the broker. See <xref:mqtt-subscription> for the topic filters, the shared subscriptions and the other MQTT consumer options.

## MQTT Sessions

Use `RequestPersistentSession()` together with a stable client identifier (`WithClientId(...)`) to resume the broker session after reconnecting and get the messages published in the meantime (QoS 1 or 2 only). See <xref:mqtt> for all the session options and their details.

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

> [!Tip]
> See <xref:error-policies> for all the available policies and options.

> [!Important]
> <xref:Silverback.Messaging.Consuming.ErrorHandling.RetryErrorPolicy> waits before retrying and may delay polling. With Kafka, ensure broker/client timeouts (for example `max.poll.interval.ms`) are configured accordingly.

### Apply Rules

Use `ApplyTo`, `Exclude`, and `ApplyWhen` to control which exceptions are handled and when. The `ApplyWhen` function receives the `IRawInboundEnvelope` (and optionally the exception), not the deserialized message.

The `MoveTo` policy accepts either the name of a topic or the friendly name of an endpoint already configured in a producer.

```csharp
.OnError(policy => policy
    .MoveTo(
        "some-other-topic",
        movePolicy => movePolicy
            .ApplyTo<MyException>()
            .ApplyWhen((envelope, exception) => envelope.Headers.Contains("x-move-on-error")))
    .ThenSkip());
```

### Callbacks

You can execute custom code when a policy is about to be applied (for logging, alerts, etc.), registering a callback such as <xref:Silverback.Messaging.Consuming.ErrorHandling.ISkipMessageErrorPolicyCallback>.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(...)
    .AddScopedErrorPolicyCallback<SkippedMessagesAlerter>();
```

```csharp
public class SkippedMessagesAlerter : ISkipMessageErrorPolicyCallback
{
    private readonly IAlertService _alertService;

    public SkippedMessagesAlerter(IAlertService alertService)
    {
        _alertService = alertService;
    }

    public Task OnSkippingMessageAsync(IRawInboundEnvelope envelope, Exception exception) =>
        _alertService.NotifyAsync(exception);
}
```

See [Callbacks](xref:error-policies#callbacks) for the available callbacks and details.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:bus>
- <xref:batch-processing>
- <xref:samples>
- Other guides in this section for broker-specific and advanced scenarios
