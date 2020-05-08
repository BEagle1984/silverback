// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Broker
{
    // TODO: Test
    internal class KafkaEventsHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaEventsHandler> _logger;

        public KafkaEventsHandler(IServiceProvider serviceProvider, ILogger<KafkaEventsHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void SetProducerEventsHandlers(
            Confluent.Kafka.ProducerBuilder<byte[], byte[]> producerBuilder) =>
            producerBuilder
                .SetStatisticsHandler((_, statistics) =>
                {
                    _logger.LogDebug($"Statistics: {statistics}");
                    CreateScopeAndPublishEvent(new KafkaStatisticsEvent(statistics));
                });

        public void SetConsumerEventsHandlers(
            KafkaInnerConsumerWrapper ownerConsumer,
            Confluent.Kafka.ConsumerBuilder<byte[], byte[]> consumerBuilder) =>
            consumerBuilder
                .SetPartitionsAssignedHandler((consumer, partitions) =>
                {
                    partitions.ForEach(partition =>
                    {
                        _logger.LogInformation("Assigned partition {topic} {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, consumer.MemberId);
                    });

                    var partitionsAssignedEvent = new KafkaPartitionsAssignedEvent(partitions, consumer.MemberId);

                    CreateScopeAndPublishEvent(partitionsAssignedEvent);

                    foreach (var topicPartitionOffset in partitionsAssignedEvent.Partitions)
                    {
                        if (topicPartitionOffset.Offset != Confluent.Kafka.Offset.Unset)
                        {
                            _logger.LogDebug("{topic} {partition} offset will be reset to {offset}.",
                                topicPartitionOffset.Topic,
                                topicPartitionOffset.Partition,
                                topicPartitionOffset.Offset);
                        }
                    }

                    return partitionsAssignedEvent.Partitions;
                })
                .SetPartitionsRevokedHandler((consumer, partitions) =>
                {
                    partitions.ForEach(partition =>
                    {
                        _logger.LogInformation("Revoked partition {topic} {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, consumer.MemberId);
                    });

                    CreateScopeAndPublishEvent(new KafkaPartitionsRevokedEvent(partitions, consumer.MemberId));
                })
                .SetOffsetsCommittedHandler((_, offsets) =>
                {
                    foreach (var offset in offsets.Offsets)
                    {
                        if (offset.Offset == Confluent.Kafka.Offset.Unset)
                            continue;

                        if (offset.Error != null && offset.Error.Code != Confluent.Kafka.ErrorCode.NoError)
                        {
                            _logger.LogError(
                                "Error occurred committing the offset {topic} {partition} @{offset}: {errorCode} - {errorReason}",
                                offset.Topic, offset.Partition, offset.Offset, offset.Error.Code,
                                offset.Error.Reason);
                        }
                        else
                        {
                            _logger.LogDebug("Successfully committed offset {topic} {partition} @{offset}",
                                offset.Topic, offset.Partition, offset.Offset);
                        }
                    }

                    CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(offsets));
                })
                .SetErrorHandler((consumer, error) =>
                {
                    // Ignore errors if not consuming anymore
                    // (lidrdkafka randomly throws some "brokers are down"
                    // while disconnecting)
                    if (!ownerConsumer.IsConsuming)
                        return;

                    var kafkaErrorEvent = new KafkaErrorEvent(error);

                    try
                    {
                        CreateScopeAndPublishEvent(kafkaErrorEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in KafkaErrorEvent subscriber.");
                    }

                    if (kafkaErrorEvent.Handled)
                        return;
                        
                    _logger.Log(
                        error.IsFatal ? LogLevel.Critical : LogLevel.Error,
                        "Error in Kafka consumer: {error} (topic(s): {topics})",
                        error,
                        ownerConsumer.Endpoint.Names);

                })
                .SetStatisticsHandler((_, statistics) =>
                {
                    _logger.LogDebug($"Statistics: {statistics}");
                    CreateScopeAndPublishEvent(new KafkaStatisticsEvent(statistics));
                });

        public void CreateScopeAndPublishEvent(IMessage message)
        {
            using var scope = _serviceProvider?.CreateScope();
            var publisher = scope?.ServiceProvider.GetRequiredService<IPublisher>();

            publisher?.Publish(message);
        }
    }
}