---
uid: chunking
---

# Chunking

Some message brokers like Apache Kafka are very efficient at handling huge amount of relatively small messages. In order to make the most out of it you may want to split your largest messages (e.g. containing binary data) into smaller chunks. Silverback can handle such scenario transparently, reassembling the message automatically in the consumer before pushing it to the internal bus.

<figure>
	<a href="~/images/diagrams/chunk-basic.png"><img src="~/images/diagrams/chunk-basic.png"></a>
    <figcaption>The messages are being split into small chunks.</figcaption>
</figure>

## Producer configuration

The producer endpoint can be configured to split the message into chunks by specifying their maximum size (in bytes).

# [Fluent](#tab/kafka-producer-fluent)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config => 
                    {
                        config.BootstrapServers = "PLAINTEXT://kafka:9092"; 
                    })
                .AddOutbound<IIntegrationEvent>(endpoint => endpoint
                    .ProduceTo("order-events")
                    .EnableChunking(500000)
                    .ProduceToOutbox());
}
```
# [Legacy](#tab/kafka-producer-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("order-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Chunk = new ChunkSettings
                    {
                        Size = 500000
                    },
                    Strategy = new OutboxProduceStrategy()
                });
}
```
***

> [!Important]
> The chunks belonging to the same message must be contiguous. It is therefore recommended to have a single producer per endpoint or partition. If using Kafka see also <xref:kafka-partitioning>.

## Consumer configuration

No particular configuration is needed in the consumer side. Silverback will automatically recognize the chunks sequence by its headers and transparently reassemble the message.

With Silverback 3.0.0 the consumer pipeline has been rewritten completely to handle this scenario in a streaming way, processing each chunk directly and applying the behaviors (such as the deserializer) on the fly. The entire original message is never stored anywhere, therefore this approach is suitable also for very large payloads.

> [!Important]
> The chunks belonging to the same message must be contiguous. It is therefore recommended to have a single producer per endpoint or partition. If using Kafka see also <xref:kafka-partitioning>.

### Incomplete sequences

Some chunks sequences may be incomplete because either the producer failed to publish all chunks or the consumer started consuming from the middle of a sequence. In both cases Silverback will silently ignore the incomplete sequences and log a warning.

### Limitations

As mentioned already, the chunks have to be written to the same partition and have to be contiguous. This is by design.

Another limitation is that the <xref:Silverback.Messaging.Inbound.ErrorHandling.MoveMessageErrorPolicy> is currently unable to move a sequence and is therefore unusable with chunked messages. This may be fixed in a future release. Please open an issue on GitHub if this is important for your use case.

## Headers

Some headers are used to describe the chunks sequence. See <xref:headers> for details.

## Samples

* <xref:sample-kafka-binaryfile>
