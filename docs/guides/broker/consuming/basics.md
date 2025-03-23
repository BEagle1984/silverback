---
uid: consuming
---

# Consuming Messages

The integration with message brokers is based on the concept of producers and consumers. Producers are responsible for sending messages to the broker, while consumers receive and process them. This guide focuses on the consuming side.

## Consumer Configuration

Silverback provides a fluent API to configure the consumer settings. The following example demonstrates how to set up a basic Kafka and MQTT cosumer.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", producer => producer
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
                .WithAtLeastOnceQoS());
```
***

The `AddConsumer` or `AddClient` method is used to configure the consumer. The `Consume` or `Consume<TMessage>` method is used to configure the specific endpoint settings and define how the messages have to be handled. The `ConsumeFrom` method specifies the topic to be consumed.

ach `AddConsumer` call will result in a consumer being instantiated. The `Consume` or `Consume<TMessage>` method can be called multiple times to configure multiple endpoints for the same consumer, each with its own settings, and leading to optimized resource usage and sometimes overall better performance in comparison to using a dedicated consumer for each endpoint. The same applies to MQTT clients.

> [!Note]
> While Kafka producers and consumers are different entities, MQTT clients are used for both producing and consuming messages.

> [!Tip]
> Assigning a name to the consumer or client and its endpoints is optional but can be useful for logging and debugging purposes, as well as for direct access to the consumer or client instance for advanced scenarios. Furthermore, it allows you to ensure that each client and endpoint is only configured once, even if you duplicate the declaration (for example if needed in multiple feature slices).

> [!Tip]
> For a more in-depth documentation about the configuration of the underlying libraries refer to the [confluent-kafka-dotnet documentation](https://docs.confluent.io/current/clients/confluent-kafka-dotnet/api/Confluent.Kafka.html) and [MQTTNet documentation]([MQTTNet documentation](https://github.com/chkr1011/MQTTnet/wiki) respectively.

## Handling Messages

The consumed messages are pushed to the [message bus](xref:bus) to be processed by the configured subscribers.

You can subscribe to plain message or to the `IInboundEnvelope<TMessage>` to get access to additional metadata such as headers, Kafka key, offset, etc.

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
        Console.WriteLine($"Kafka Key: {envelope.GetKafkaKey()}");
    }
}
```

Messages can of course be processed synchronously or asynchronously (returning a `Task` or a `ValueTask`). The consumer will automatically handle the message acknowledgment or offset storage and consider the message as successfully processed once the subscriber method returns without throwing an exception.

The same message can be processed by multiple subscribers but this can lead to some issues if the subscribers are not idempotent and is generally not recommended. Silverback won't track the single subscribers and consider the processing failed if any of them throws an exception.

Messages can also be processed in [batch](xref:batch-processing) to avoid micro-transaction overhead and improve performance.

## Parallelism

The Kafka and MQTT consumers work fundamentally differently and have therefore a substantially different approach to parallelism.

By default, the Kafka consumer creates a separate queue for each partition of every topic being consumed, allowing messages to be processed in parallel. The number of partitions primarily determines the degree of parallelism, as messages within the same partition must be processed sequentially (this is a fundamental aspect of the Kafka protocol). However, you can limit the number of parallel tasks if needed.

```csharp
.AddConsumer("consumer1", producer => producer
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .LimitParallelism(3)
        .ConsumeFrom("my-topic-1")
        .ConsumeFrom("my-topic-2")))
```

In some cases you might want to process all messages from the same consumer in a single stream, disabling parallelism. This can be achieved using the `ProcessAllPartitionsTogether` configuration method.

```csharp
.AddConsumer("consumer1", producer => producer
    .Consume<MyMessage>("endpoint1", endpoint => endpoint
        .ProcessAllPartitionsTogether()
        .ConsumeFrom("my-topic-1")
        .ConsumeFrom("my-topic-2")))
```

The MQTT consumer on the other hand will process the messages sequentially by default, but parallel processing can be enabled with the `EnableParallelProcessing` method, specifying the desired degree of parallelism.

```csharp
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .EnableParallelProcessing(10)
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("messages/topic1")
                .ConsumeFrom("messages/topic2")
                .WithAtLeastOnceQoS());
```

## Error Handling

By default, Silverback will stop the consumer if an exception is thrown by the subscriber processing the consumed message. This behavior can be customized by defining a customized error policy.

The built-in policies are:
* <xref:Silverback.Messaging.Consuming.ErrorHandling.StopConsumerErrorPolicy> (default)
* <xref:Silverback.Messaging.Consuming.ErrorHandling.SkipMessageErrorPolicy>
* <xref:Silverback.Messaging.Consuming.ErrorHandling.RetryErrorPolicy>
* <xref:Silverback.Messaging.Consuming.ErrorHandling.MoveMessageErrorPolicy>
* <xref:Silverback.Messaging.Consuming.ErrorHandling.ErrorPolicyChain>

The policies can be combined to create a chain of error handling strategies.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", producer => producer
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
                .WithAtLeastOnceQoS());
```
***

> [!Important]
> If the processing still fails after the last policy is applied the exception will be returned to the consumer, causing it to stop.

> [!Important]
> The <xref:Silverback.Messaging.Consuming.ErrorHandling.RetryErrorPolicy> will prevent the message broker to be polled for the duration of the configured delay, which could lead to a timeout. With Kafka you should for example set the `max.poll.interval.ms` settings to a higher value.

### Apply Rules

Use [ApplyTo](xref:Silverback.Messaging.Configuration.ErrorPolicyBaseBuilder`1#Silverback_Messaging_Configuration_ErrorPolicyBaseBuilder_1_ApplyTo_System_Type_) and [Exclude](xref:Silverback.Messaging.Configuration.ErrorPolicyBaseBuilder`1#Silverback_Messaging_Configuration_ErrorPolicyBaseBuilder_1_Exclude_System_Type_) methods to fine-tune which exceptions must be handled by the error policy or take advantage of [ApplyWhen](xref:Silverback.Messaging.Configuration.ErrorPolicyBaseBuilder`1#Silverback_Messaging_Configuration_ErrorPolicyBaseBuilder_1_ApplyWhen_System_Func_Silverback_Messaging_Messages_IRawInboundEnvelope_System_Boolean__) to specify a custom apply rule.

```csharp
.OnError(policy => policy
    .MoveToKafkaTopic(
        moveEndpoint => moveEndpoint.ProduceTo("some-other-topic"),
        movePolicy => movePolicy
            .ApplyTo<MyException>()
            .ApplyWhen((msg, ex) => msg.Xy == myValue))
    .ThenSkip());
```

### Publishing Events

Messages can be published when a policy is applied, in order to execute custom code.

```csharp
.OnError(policy => policy
    .Retry(3, TimeSpan.FromSeconds(1))
    .ThenSkip(skipPolicy => skipPolicy
        .Publish(msg => new ProcessingFailedEvent(msg))))
}
```

These messages can be handled by a subscriber to log the error, send an alert, etc.

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:bus> guide
* <xref:consuming> guide
* <xref:samples>
* Other guides in this section for in-depth information about the consumer capabilities
