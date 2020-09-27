// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Broker
{
    // TODO: Test
    internal class KafkaEventsHandler
    {
        private readonly IServiceProvider? _serviceProvider;

        private readonly ISilverbackIntegrationLogger<KafkaEventsHandler> _logger;

        public KafkaEventsHandler(
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<KafkaEventsHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public void SetProducerEventsHandlers(IConfluentProducerBuilder producerBuilder) =>
            producerBuilder.SetStatisticsHandler(OnProducerStatistics);

        public void SetConsumerEventsHandlers(
            KafkaConsumer ownerConsumer,
            IConfluentConsumerBuilder consumerBuilder) =>
            consumerBuilder
                .SetStatisticsHandler(OnConsumerStatistics)
                .SetPartitionsAssignedHandler(OnPartitionsAssigned)
                .SetPartitionsRevokedHandler(OnPartitionsRevoked)
                .SetOffsetsCommittedHandler(OnOffsetsCommitted)
                .SetErrorHandler((_, error) => OnConsumerError(ownerConsumer, error));

        public void CreateScopeAndPublishEvent(IMessage message)
        {
            try
            {
                using (var scope = _serviceProvider?.CreateScope())
                {
                    var publisher = scope?.ServiceProvider.GetRequiredService<IPublisher>();

                    publisher?.Publish(message);
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore the ObjectDisposedException that may be thrown when disconnecting
            }
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private void OnProducerStatistics(IProducer<byte[]?, byte[]?> producer, string statistics)
        {
            _logger.LogDebug(KafkaEventIds.ProducerStatisticsReceived, $"Statistics: {statistics}");
            CreateScopeAndPublishEvent(new KafkaStatisticsEvent(statistics));
        }

        private void OnConsumerStatistics(IConsumer<byte[], byte[]> consumer, string statistics)
        {
            _logger.LogDebug(KafkaEventIds.ConsumerStatisticsReceived, $"Statistics: {statistics}");
            CreateScopeAndPublishEvent(new KafkaStatisticsEvent(statistics));
        }

        private IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
            IConsumer<byte[], byte[]> consumer,
            List<TopicPartition> partitions)
        {
            partitions.ForEach(
                partition =>
                {
                    _logger.LogInformation(
                        KafkaEventIds.PartitionsAssigned,
                        "Assigned partition {topic} {partition}, member id: {memberId}",
                        partition.Topic,
                        partition.Partition,
                        consumer.MemberId);
                });

            var partitionsAssignedEvent = new KafkaPartitionsAssignedEvent(partitions, consumer.MemberId);

            CreateScopeAndPublishEvent(partitionsAssignedEvent);

            foreach (var topicPartitionOffset in partitionsAssignedEvent.Partitions)
            {
                if (topicPartitionOffset.Offset != Offset.Unset)
                {
                    _logger.LogDebug(
                        KafkaEventIds.PartitionOffsetReset,
                        "{topic} {partition} offset will be reset to {offset}.",
                        topicPartitionOffset.Topic,
                        topicPartitionOffset.Partition,
                        topicPartitionOffset.Offset);
                }
            }

            return partitionsAssignedEvent.Partitions;
        }

        private void OnPartitionsRevoked(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> partitions)
        {
            partitions.ForEach(
                partition =>
                {
                    _logger.LogInformation(
                        KafkaEventIds.PartitionsRevoked,
                        "Revoked partition {topic} {partition}, member id: {memberId}",
                        partition.Topic,
                        partition.Partition,
                        consumer.MemberId);
                });

            CreateScopeAndPublishEvent(new KafkaPartitionsRevokedEvent(partitions, consumer.MemberId));
        }

        private void OnOffsetsCommitted(IConsumer<byte[], byte[]> consumer, CommittedOffsets offsets)
        {
            foreach (var offset in offsets.Offsets)
            {
                if (offset.Offset == Offset.Unset)
                    continue;

                if (offset.Error != null && offset.Error.Code != ErrorCode.NoError)
                {
                    _logger.LogError(
                        KafkaEventIds.KafkaEventsHandlerErrorWhileCommittingOffset,
                        "Error occurred committing the offset {topic} {partition} @{offset}: {errorCode} - {errorReason}",
                        offset.Topic,
                        offset.Partition,
                        offset.Offset,
                        offset.Error.Code,
                        offset.Error.Reason);
                }
                else
                {
                    _logger.LogDebug(
                        KafkaEventIds.OffsetCommitted,
                        "Successfully committed offset {topic} {partition} @{offset}",
                        offset.Topic,
                        offset.Partition,
                        offset.Offset);
                }
            }

            CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(offsets));
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private void OnConsumerError(KafkaConsumer consumer, Error error)
        {
            // Ignore errors if not consuming anymore
            // (lidrdkafka randomly throws some "brokers are down"
            // while disconnecting)
            if (!consumer.IsConnected)
                return;

            var kafkaErrorEvent = new KafkaErrorEvent(error);

            try
            {
                CreateScopeAndPublishEvent(kafkaErrorEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    KafkaEventIds.KafkaErrorHandlerError,
                    ex,
                    "Error in KafkaErrorEvent subscriber.");
            }

            if (kafkaErrorEvent.Handled)
                return;

            _logger.Log(
                error.IsFatal ? LogLevel.Critical : LogLevel.Error,
                KafkaEventIds.ConsumerError,
                "Error in Kafka consumer: {error} (topic(s): {topics})",
                error,
                ((KafkaConsumerEndpoint)consumer.Endpoint).Names);
        }
    }
}
