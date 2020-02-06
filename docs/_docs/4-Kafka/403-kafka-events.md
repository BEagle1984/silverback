---
title: Kafka Events
permalink: /docs/kafka/events
toc: true
---

To let you catch the Kafka events (fired for example when the partions are assigned or revoked) they are mapped to some events that are published to the internal bus.

## Consumer events

Event | Description
:-- | :--
`KafkaPartitionsAssignedEvent` | The event fired when a new consumer group partition assignment has been received by a consumer. Corresponding to each of this events there will be a `KafkaPartitionsRevokedEvent`. This event is important if you need to manually set the start offset (see the sample code below in the samples section).
`KafkaPartitionsRevokedEvent` | The event fired prior to a group partition assignment being revoked. Corresponding to each of this events there will be a `KafkaPartitionsAssignedEvent`.
`KafkaOffsetsCommittedEvent` | The event fired to report the result of the offset commits.
`KafkaErrorEvent` | The event fired when an error is reported by the `Confluent.Kafka.Consumer` (e.g. connection failures or all brokers down). Note that the system (either the Kafka client itself or Silverback) will try to automatically recover from all errors automatically, so these errors have to be considered purely informational.
`KafkaStatisticsEvent` | The event fired when statistics are received. Statistics are provided as a JSON formatted string as defined in the [librdkafka documentation](https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md). You can enable statistics and set the statistics interval using the `StatisticsIntervalMs` configuration parameter (disabled by default).


## Sample use cases

### Setting starting offsets on partitions assignment

In the following example the `KafkaPartitionsAssignedEvent` is subscribed in order to reset the start offsets and replay the past messages.

```c#
public void OnPartitionsAssigned(KafkaPartitionsAssignedEvent message)
{
    message.Partitions = message.Partitions
        .Select(topicPartitionOffset =>
            new TopicPartitionOffset(
                topicPartitionOffset.TopicPartition,
                Offset.Beginning))
        .ToList();
}
```