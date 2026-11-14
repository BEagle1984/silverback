---
uid: producing-chunking
description: "Split large messages into chunks on the producer side to stay under broker size limits such as Kafka's, with the consumer reassembling them automatically."
---

# Producing Chunked Messages

When dealing with large messages, it might be necessary to split them into smaller parts to avoid issues with the broker or the consumer. This process is called **chunking** and is especially useful with Kafka, which has a maximum message size limit, or with the MQTT brokers limiting the message size.

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png" alt="Chunking diagram: a large message is split into smaller chunks."></a>
    <figcaption>The messages are being split into small chunks.</figcaption>
</figure>

The producer automatically adds the necessary headers to the messages to allow the consumer to reassemble the chunks. The consumer will then automatically reassemble the message before pushing it to subscribers and doesn't require any additional configuration.

## Producer Configuration

# [Kafka](#tab/kafka)
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
# [MQTT](#tab/mqtt)
```csharp
services.AddSilverback()
    .WithConnectionToMessageBroker(options => options.AddMqtt())
    .AddMqttClients(clients => clients
        .ConnectViaTcp("localhost")
        .AddClient("my-client", client => client
            .WithClientId("client1")
            .Produce<MyMessage>("endpoint1", endpoint => endpoint
                .ProduceTo("messages/my")
                .WithAtLeastOnceQoS()
                .EnableChunking(100000))));
```
***

> [!Important]
> The chunks belonging to the same message must be contiguous. It is therefore recommended to have a single producer per endpoint or partition. If using Kafka see also <xref:kafka-partitioning>.

> [!Important]
> With MQTT, chunking requires MQTT 5 and can't be enabled for retained messages (see `Retain`), since the broker retains only the last message of each topic: a client subscribing later would receive the last chunk alone.

## Additional Resources

- [API Reference](xref:Silverback)
- <xref:consuming-chunking>
- <xref:default-headers>
