---
uid: producing-tombstone
---

# Producing Tombstone Messages

A tombstone is a Kafka message with a `null` payload. It is typically used to delete a record in a compacted topic, identified by its key.

A tombstone always needs a key. You can produce one either by publishing a `Tombstone<TMessage>` or by publishing a `null` payload and setting the key on the outbound envelope.

## Using `WrapAndPublishAsync` / `WrapAndPublishBatchAsync`

Publish a `null` payload and set the Kafka key:

```csharp
await publisher.WrapAndPublishAsync<MyMessage>(
    message: null,
    envelope => envelope.SetKafkaKey("42"));
```

## Publishing a `Tombstone<TMessage>`

Use <xref:Silverback.Messaging.Messages.Tombstone`1> to publish a tombstone explicitly. The message type is used for routing.

```csharp
await publisher.PublishAsync(new Tombstone<MyMessage>("42"));
```

## Endpoint Configuration

Configure the endpoint for the message type (`TMessage`) so that both regular messages and tombstones are routed to the same topic.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))));
```

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming-tombstone>
