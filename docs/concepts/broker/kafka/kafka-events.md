---
uid: kafka-events
---

# Kafka Events

The underlying library ([Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet)) uses some events to let you catch important information and interact with the partitions assignment process.
Silverback proxies those events to give you full access to those features.

## Consumer events

The event handlers are configured via the <xref:Silverback.Messaging.KafkaEvents.KafkaConsumerEventsHandlers> and the related methods in the <xref:Silverback.Messaging.Configuration.IKafkaConsumerEndpointBuilder>.

### Offset reset example

In the following example the partitions assigned event is subscribed in order to reset the start offsets and replay the past messages.

# [EndpointsConfigurator (fluent)](#tab/offset-reset-fluent)
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
                    .ConsumeFrom("some-topic")
                    .OnPartitionsAssigned(
                        (partitions, _) =>
                            partitions.Select(
                                topicPartition => new TopicPartitionOffset(
                                    topicPartition,
                                    Offset.Beginning)))));
}
```
# [EndpointsConfigurator (legacy)](#tab/offset-reset-legacy)
```csharp
public class MyEndpointsConfigurator : IEndpointsConfigurator
{
    public void Configure(IEndpointsConfigurationBuilder builder) =>
        builder
            .AddInbound(
                new KafkaConsumerEndpoint("some-topic")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        BootstrapServers = "PLAINTEXT://kafka:9092"
                    },
                    Events =
                    {
                        PartitionsAssignedHandler =
                            (partitions, _) =>
                                partitions.Select(
                                    topicPartition => new TopicPartitionOffset(
                                        topicPartition,
                                        Offset.Beginning))
                    }
                });
}
```
***

## Producer events

The event handlers are configured via the <xref:Silverback.Messaging.KafkaEvents.KafkaProducerEventsHandlers> and the related methods in the <xref:Silverback.Messaging.Configuration.IKafkaProducerEndpointBuilder>.
