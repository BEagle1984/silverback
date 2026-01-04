---
uid: kafka-partitioning
---

# Kafka Keys and Partitioning

Kafka partitions determine message ordering and parallelism. By default, Kafka distributes messages across partitions, but you can control routing via message keys or explicit partition selection.

## Key-Based Partitioning

By default (no explicit partition), Kafka derives the partition from the message key.

- If you set a key, messages with the same key are routed to the same partition and keep their relative order.
- If you don't set a key, a key may be generated and partitioning will be effectively random.

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
