---
uid: kafka-subscription
---

# Kafka Subscription and Partition Assignment

Silverback supports the standard Kafka consumer group subscription flow and manual (static) partition assignment.

## Subscribe (Consumer Group)

In consumer group mode, Kafka assigns partitions automatically and balances them across instances with the same group id.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

## Static Assignment

You can manage partition assignment manually by specifying the partitions to consume.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic", 0, 3, 5)))));
```

> [!Note]
> The example above assigns partitions 0, 3, and 5 of `my-topic` to this consumer endpoint. Other overloads let you specify starting offsets or consume fixed partitions across multiple topics.

## Static Assignment from Topic Metadata

If you don't want to hardcode partition numbers, you can retrieve topic metadata and select partitions programmatically.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom(
                    "my-topic",
                    partitions => partitions
                        .Where(p => p.Partition % 2 == 0)
                        .Select(p => new TopicPartitionOffset(p, Offset.Beginning))))));
```

## Partition Processing and Concurrency

By default, partitions are processed in parallel (one queue per partition). Messages in the same partition are always processed sequentially.

You can limit concurrency using `LimitParallelism` or disable partition-based parallelism using `ProcessAllPartitionsTogether`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", consumer => consumer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ProcessAllPartitionsTogether()
                .ConsumeFrom("my-topic"))));
```

## Callbacks

Use broker callbacks to react to partition assignment/revocation events. See <xref:broker-callbacks>.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming>
- <xref:broker-callbacks>
