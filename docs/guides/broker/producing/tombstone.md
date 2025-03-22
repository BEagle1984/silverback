---
uid: producing-tombstone
---

# Producing Tombstone Messages

A tombstone message is a message with a `null` payload. It is used to signal that a message with a specific key should be deleted. The key is used to identify the message to be deleted.

There are mainly two ways to produce tombstone messages:
* Using `WrapAndPublish` or `WrapAndPublishBatch` methods
* Publishing a <xref:Silverback.Messaging.Messages.Tombstone`1>

## Using `WrapAndPublish` or `WrapAndPublishBatch`

The `WrapAndPublish` and `WrapAndPublishBatch` methods can be used to produce a message with `null` payload and a specific key (or headers). The code snippet below shows how to produce a tombstone message using the `WrapAndPublishAsync` method.

```csharp
await publisher.WrapAndPublishAsync<TestEventOne>(null, envelope => envelope.SetKafkaKey("42"));
```

## Publishing a Tombstone<TMessage>

The <xref:Silverback.Messaging.Messages.Tombstone`1> model can be used to produce a tombstone message. The type parameter `TMessage` is used to distinguish between different tombstone messages to be routed to different endpoints and the constructor parameter is used to specify the key of the message to be deleted.

```csharp
await _publisher.PublishAsync(new Tombstone<Product>(productId));
```

In the endpoint configuration you can map the type corresponding to the `TMessage` parameter, so with a single registration you can handle regular messages and tombstones.

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<Products>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic"))));
```

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:consuming-tombstone> guide
