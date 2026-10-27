---
uid: kafka-partitioning
---

# Kafka Keys and Partitioning

Kafka partitions determine message ordering and parallelism. By default, Kafka distributes messages across partitions, but you can control routing via message keys or explicit partition selection.

## Key-Based Partitioning

By default (no explicit partition), Kafka derives the partition from the message key.

- If you set a key, messages with the same key are routed to the same partition and keep their relative order.
- If you don't set a key, the key is `null` and the partition is chosen by the Kafka partitioner.

### Using `WrapAndPublishAsync` / `WrapAndPublishBatchAsync`

Set the key by configuring the outbound envelope:

```csharp
await publisher.WrapAndPublishAsync(
    new MyMessage { Content = "Hello, World!" },
    envelope => envelope.SetKafkaKey("42"));
```

### Via Endpoint Configuration

Define the Kafka key in the endpoint configuration using `SetKafkaKey`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .SetKafkaKey(message => message.Id))));
```

### Using the `KafkaKeyMemberAttribute`

Alternatively, decorate one or more properties of the message model with the <xref:Silverback.Messaging.Messages.KafkaKeyMemberAttribute>.

```csharp
public class OrderEvent
{
    [KafkaKeyMember]
    public string OrderId { get; set; }

    public string Status { get; set; }
}
```

If a single property is decorated, its value is used as key (e.g. `42`). If multiple properties are decorated, the key is built concatenating their names and values (e.g. `OrderId=42,LineId=3`). Empty values are ignored and the key is `null` if none of the decorated properties has a value.

> [!Note]
> A key set explicitly via `SetKafkaKey` (in the endpoint configuration or on the envelope) takes precedence over the attribute.

## Custom Partitioning

If you need explicit control over partition selection, use the `ProduceTo` overload that accepts a partition selector.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic", message => message.Id % 3))));
```

## Static Partitioning

To send all messages to a fixed partition, set the `partition` parameter.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic", partition: 3))));
```

## Additional Resources

- [API Reference](xref:Silverback)
