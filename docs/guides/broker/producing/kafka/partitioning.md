---
uid: kafka-partitioning
---

# Kafka Key and Partitioning

The partitioning of messages in Kafka is a crucial aspect of how data is distributed across the cluster. By default, Kafka uses a round-robin approach to distribute messages across partitions, but you can control this behavior by specifying a key for your messages.

## Key-Based Partitioning

When you produce a message with a key, Kafka uses the key to determine which partition the message will be sent to. This ensures that all messages with the same key are sent to the same partition, maintaining their order.

### Using `WrapAndPublish` or `WrapAndPublishBatch`

You can set the key directly using the `WrapAndPublish`/`WrapAndPublishAsync` or `WrapAndPublishBatch`/`WrapAndPublishBatchAsync` methods and setting the key in each envelope.

```csharp
await publisher.WrapAndPublishAsync<TestEventOne>(
    new MyMessage { Content = "Hello, World!" },
    envelope => envelope.SetKafkaKey("42"));
```

### Via Endpoint Configuration

The Kafka key can be defined in the endpoint configuration using one of the several overloads of the `SetKafkaKey` method to set the key based on a property of the message or the envelope.

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

If you need more control over how messages are partitioned, you can implement a custom partitioner using the appropriate overload of the `ProduceTo` method. This allows you to define your own logic for determining the partition based on the message or envelope properties.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic", messae => message.Id % 3))));
```

## Static Partitioning

If you want to send all messages to a specific partition, you can use the appropriate overload of the `ProduceTo` method that accepts a static partition number.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic", 3))));
```

## Additional Resources

* [API Reference](xref:Silverback)
