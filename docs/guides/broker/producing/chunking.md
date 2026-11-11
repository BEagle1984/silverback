---
uid: producing-chunking
description: "Split large messages into chunks on the producer side to stay under broker size limits such as Kafka's, with the consumer reassembling them automatically."
---

# Producing Chunked Messages

When dealing with large messages, it might be necessary to split them into smaller parts to avoid issues with the broker or the consumer. This process is called **chunking** and is especially useful when using Kafka, as it has a maximum message size limit.

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png" alt="Chunking diagram: a large message is split into smaller chunks."></a>
    <figcaption>The messages are being split into small chunks.</figcaption>
</figure>

The producer automatically adds the necessary headers to the messages to allow the consumer to reassemble the chunks. The consumer will then automatically reassemble the message before pushing it to subscribers and doesn't require any additional configuration.

## Producer Configuration

```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddKafka())
    .AddKafkaClients(clients => clients
        .WithBootstrapServers("PLAINTEXT://localhost:9092")
        .AddProducer("producer1", producer => producer
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("my-topic")
                .EnableChunking(500000))));
```

> [!Important]
> The chunks belonging to the same message must be contiguous. It is therefore recommended to have a single producer per endpoint or partition. If using Kafka see also <xref:kafka-partitioning>.

With MQTT, chunking requires MQTT 5 and is useful with the brokers limiting the message size (e.g. cloud brokers allowing only small messages). The chunks of the same message are correlated by the `x-chunk-message-id` user property and must be produced to the same topic with the same QoS, since MQTT only preserves the order of those messages. Chunking can't be enabled for retained messages, since the broker retains only the last chunk, and the chunks can't be consumed through a shared subscription, which spreads them across the consumers. See also <xref:consuming-chunking> for the requirements on the consumer side, which can't use batch processing.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming-chunking>
- <xref:default-headers>
