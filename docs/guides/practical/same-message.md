---
uid: same-message
---

# Consuming the Same Message from Multiple Topics

The same message type is often published to more than one topic: one topic per region or tenant, a legacy topic kept alive during a migration, a retry topic, etc. Silverback lets you consume all of them and handle the messages with the same subscriber.

## Single Endpoint, Multiple Topics

If all topics share the same settings, list them in a single endpoint. With MQTT, you can also use the topic filter wildcards (`+` and `#`).

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("order-processor")
            .Consume<OrderEvent>("orders", endpoint => endpoint
                .ConsumeFrom("orders-eu", "orders-us", "orders-apac"))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("client1", client => client
            .WithClientId("order-processor")
            .Consume<OrderEvent>("orders", endpoint => endpoint
                .ConsumeFrom("orders/eu", "orders/us", "stores/+/orders")
                .WithAtLeastOnceQoS())));
```
***

## Multiple Endpoints

When the topics need different settings (a different deserializer configuration, error policy, QoS, etc.), configure an endpoint per topic. The endpoints can still belong to the same consumer/client and target the same message type.

# [Kafka](#tab/kafka)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .WithGroupId("order-processor")
            .Consume<OrderEvent>("orders", endpoint => endpoint
                .ConsumeFrom("orders")
                .OnError(policy => policy.Retry(3).ThenSkip()))
            .Consume<OrderEvent>("legacy-orders", endpoint => endpoint
                .ConsumeFrom("legacy-orders")
                .DeserializeJson(deserializer => deserializer
                    .Configure(options =>
                    {
                        options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                    }))
                .OnError(policy => policy.Skip()))));
```
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("client1", client => client
            .WithClientId("order-processor")
            .Consume<OrderEvent>("orders", endpoint => endpoint
                .ConsumeFrom("orders/#")
                .WithExactlyOnceQoS()
                .OnError(policy => policy.Retry(3).ThenSkip()))
            .Consume<OrderEvent>("legacy-orders", endpoint => endpoint
                .ConsumeFrom("legacy/orders")
                .WithAtMostOnceQoS()
                .OnError(policy => policy.Skip()))));
```
***

> [!Note]
> A topic can be consumed by only one endpoint per consumer/client: Silverback rejects the configuration if two endpoints of the same consumer/client point to the same topic. To consume the same topic twice, see <xref:multiple-consumers>.

## Handle the Messages

A subscriber for `OrderEvent` receives the messages from all the configured topics. If you need to know where a message comes from, subscribe to the <xref:Silverback.Messaging.Messages.IInboundEnvelope`1> instead:

```csharp
public class OrderSubscriber
{
    public async Task HandleAsync(IInboundEnvelope<OrderEvent> envelope)
    {
        // The actual topic the message was consumed from (also with MQTT wildcards)
        string topic = envelope.Endpoint.RawName;

        // The name of the endpoint (e.g. "orders" or "legacy-orders")
        string? endpointName = envelope.Endpoint.Configuration.FriendlyName;

        // ...process envelope.Message
    }
}
```

With Kafka, `envelope.GetKafkaOffset()` additionally returns the topic, partition and offset of the consumed message.

## Caveats

- **Ordering**: Kafka guarantees ordering only within a partition and MQTT only within a topic. There is no ordering guarantee across the different topics.
- **Duplicates**: if the producer publishes the same message to several topics (e.g., while migrating from a legacy topic), it will be processed once per topic. Make the subscriber idempotent, for example by storing the IDs of the processed messages.
- **Overlapping MQTT topic filters**: avoid subscribing to overlapping topic filters in the same client (e.g., `orders/#` and `orders/eu`). Depending on the broker, the message may be delivered once per matching subscription. Silverback maps each message to a single endpoint, giving precedence to the endpoint with the exact topic name and otherwise using the first matching wildcard subscription.
- **Kafka parallelism**: each partition of each topic is processed independently, unless `ProcessAllPartitionsTogether()` is set or the parallelism is limited via `LimitParallelism(...)`. See <xref:consuming>.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming>
- <xref:kafka-subscription>
- <xref:deserialization>
- <xref:multiple-consumers>
