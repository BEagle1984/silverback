---
uid: kafka-partitioning
---

# Kafka Partitioning and Message Key

## Producer

### Destination partition

If the destination topic contains multiple partitions, the destination partition is picked according to the hash of the [message key](#message-key). If no explicit message key was set, a random one is generated, resulting in the messages being randomly spread across the partitions.

You can override this default behavior explicitly setting the target partition in the endpoint. The endpoint can be statically defined like in the following snippet or resolved via [dynamic routing](xref:outbound-routing).

# [Fluent](#tab/destination-partition-fluent)
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
                    .ProduceTo("order-events", 2))); // <- partition 2
}
```
# [Legacy](#tab/destination-partition-legacy)
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
                    Partition = 2
                });
}
```
***

Producing to a fixed partition may be required in the case you have multiple producers to the same topic and you have to prevent the messages from the different clients to be interleaved (e.g. because you are relying on sequences, like [chunking](xref:chunking)).

### Message key

Apache Kafka require a message key for different purposes, such as:
* **Partitioning**: Kafka can guarantee ordering only inside the same partition and it is therefore important to be able to route correlated messages into the same partition. To do so you need to specify a key for each message and Kafka will put all messages with the same key in the same partition.
* **Compacting topics**: A topic can be configured with `cleanup.policy=compact` to instruct Kafka to keep only the latest message related to a certain object, identified by the message key. In other words Kafka will retain only 1 message per each key value.

<figure>
	<a href="~/images/diagrams/kafka-key.png"><img src="~/images/diagrams/kafka-key.png"></a>
    <figcaption>The messages with the same key are guaranteed to be written to the same partition.</figcaption>
</figure>

Silverback will always generate a message key (same value as the `x-message-id` [header](xref:headers)) but you can also generate your own key, either adding an enricher to the <xref:Silverback.Messaging.IProducerEndpoint> or decorating the properties that must be part of the key with <xref:Silverback.Messaging.Messages.KafkaKeyMemberAttribute>.

#### Using enricher

# [Fluent](#tab/enricher-fluent)
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
                .AddOutbound<InventoryEvent>(endpoint => endpoint
                    .ProduceTo("inventory-events")
                    .WithKafkaKey<InventoryEvent>(
                        envelope => envelope.Message?.ProductId)));
}
```
# [Legacy](#tab/enricher-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddOutbound<InventoryEvent>(
                new KafkaProducerEndpoint("inventory-events")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    MessageEnrichers = new List<IOutboundMessageEnricher>
                    {
                        new OutboundMessageKafkaKeyEnricher<InventoryEvent>(
                            envelope => envelope.Message?.ProductId)
                    }
                });
}
```
***

#### Using KafkaKeyMemberAttribute
```csharp
public class MultipleKeyMembersMessage : IIntegrationMessage
{
    public Guid Id { get; set; }

    [KafkaKeyMember]
    public string One { get; set; }
    
    [KafkaKeyMember]
    public string Two { get; set; }

    public string Three { get; set; }
}
```

> [!Note]
> The message key will also be received as header (see <xref:headers> for details).

## Consumer

### Partitions processing 

While using a single poll loop, Silverback processes the messages consumed from each Kafka partition independently and concurrently.

By default up to 10 messages/partitions are processed concurrently (per topic). This value can be tweaked in the endpoint configuration or disabled completely.

# [Fluent](#tab/concurrency-fluent)
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
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("order-events")
                    .LimitParallelism(2)
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer";
                        })
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom("inventory-events")
                    .ProcessAllPartitionsTogether()
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer";
                        })));
}
```
# [Legacy](#tab/concurrency-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("order-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                    },
                    MaxDegreeOfParallelism = 2 
                })
            .AddInbound(
                new KafkaConsumerEndpoint("inventory-events")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                    },
                    ProcessPartitionsIndependently = false 
                });
}
```
***

### Manual partitions assignment

In some cases you don't want to let the broker randomly distribute the partitions among the consumers. This is especially useful when dealing with large sequences (e.g. large messages/files being [chunked](xref:chunking)), to prevent that a rebalance occurs in the middle of a sequence, forcing the consumer to abort and restart from the beginning.

# [Fluent](#tab/assignment-fluent)
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
                .AddInbound(endpoint => endpoint
                    .ConsumeFrom(
                        new TopicPartition("order-events", 0),
                        new TopicPartition("order-events", 1))
                    .Configure(config =>
                        {
                            config.GroupId = "my-consumer";
                        })));
}
```
# [Legacy](#tab/assignment-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint(
                    new TopicPartition("order-events", 0),
                    new TopicPartition("order-events", 1))
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092",
                        GroupId = "my-consumer",
                    } 
                });
}
```
***

## Samples

* <xref:sample-kafka-binaryfile>
