---
uid: producing-chunking
---

# Producing Chunked Messages

When dealing with large messages, it might be necessary to split them into smaller parts to avoid issues with the broker or the consumer. This process is called **chunking** and is especially useful when using Kafka, as it has a maximum message size limit.

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png"></a>
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

## Additional Resources

* [API Reference](xref:Silverback)
* <xref:consuming-chunking> guide
* <xref:default-headers> guide
