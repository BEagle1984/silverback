---
uid: kafka-subscription
---

# Kafka Subscription and Partitions Assignment

There are multiple ways to subscribe to Kafka topics and manage partitions assignment in Silverback. Either via the usual broker-driven subscription or manually assigning partitions.

## Subscribe

The consumer group will be used to manage partitions assignment automatically and distribute the load across the consumer instances.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", producer => producer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic"))));
```

## Static Assignment

The partitions assignment can be managed manually by specifying the partitions to be consumed and optionally the offset to start from.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", producer => producer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom("my-topic", [0, 3, 5]))));
```

> [!Note]
> The example above will assign partitions 0, 3 and 5 of the topic `my-topic` to the consumer. Other overloads exist to specify the offset to start from or consume partitions from different topics.

## Static Assignment from Topic Metadata

To avoid having to hardcode the partitions to be consumed, Silverback can read the topic metadata and use a custom function to determine the actual partitions to be consumed.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", producer => producer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ConsumeFrom(
                    "my-topic",
                    partitions => partitions
                        .Where(partition => partition.Partition % 2 == 0)
                        .Select(partition => new TopicPartitionOffset(partition, Offset.Beginning)))));
```

## Partitions Processing and Concurrency

By default, the partitions are processed in parallel, meaning that a separate queue is created for each partition, leading to messages from different partitions being processed concurrently in different threads.

The parallelism can be limited using `LimitParallelism` or completely disabled using `ProcessAllPartitionsTogether`.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddConsumer("consumer1", producer => producer
            .Consume<MyMessage>("endpoint1", endpoint => endpoint
                .ProcessAllPartitionsTogether())));
```

## Callbacks

Callbacks can be used to perform additional actions when partitions are assigned or revoked. See <xref:broker-callbacks> for more information.

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:broker-basics> guide
* <xref:broker-callbacks> guide
