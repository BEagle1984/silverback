---
uid: kafka-events
---

# Kafka Events

The underlying library ([Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet)) uses some events to let you catch important information, interact with the partitions assignment process, etc.

Silverback proxies those events to give you full access to those features.

## Consumer events

These callbacks are available:
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionsAssignedCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionsRevokedCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaOffsetCommittedCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerErrorCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerStatisticsCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaConsumerLogCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaPartitionEofCallback>

### Offset reset example

In the following example the partitions assigned event is subscribed in order to reset the start offsets and replay the past messages.

# [Startup](#tab/offset-reset-startup)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddSingletonBrokerCallbackHandler<ResetOffsetPartitionsAssignedCallbackHandler>();
    }
}
```
# [ResetOffsetPartitionsAssignedCallbackHandler](#tab/offset-reset-handler)
```csharp
public class ResetOffsetPartitionsAssignedCallbackHandler
    : IKafkaPartitionsAssignedCallback
{
    public IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
        IReadOnlyCollection<TopicPartition> topicPartitions,
        KafkaConsumer consumer) =>
        topicPartitions.Select(
            topicPartition => new TopicPartitionOffset(topicPartition, Offset.Beginning));
}
```
***

## Producer events

These callbacks are available:
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaProducerStatisticsCallback>
* <xref:Silverback.Messaging.Broker.Callbacks.IKafkaProducerLogCallback>

## See also

<xref:broker-callbacks>
