// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.ConfluentWrappers;
using Silverback.Messaging.KafkaEvents.Statistics;

namespace Silverback.Messaging.KafkaEvents
{
    internal static class KafkaEventsBinder
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static void SetEventsHandlers(
            this IConfluentProducerBuilder producerBuilder,
            KafkaProducer producer,
            ISilverbackIntegrationLogger logger) =>
            producerBuilder
                .SetStatisticsHandler((_, statistics) => OnStatistics(statistics, producer, logger))
                .SetLogHandler((_, logMessage) => OnLog(logMessage, producer, logger));

        public static void SetEventsHandlers(
            this IConfluentConsumerBuilder consumerBuilder,
            KafkaConsumer consumer,
            ISilverbackIntegrationLogger logger) =>
            consumerBuilder
                .SetStatisticsHandler((_, statistics) => OnStatistics(statistics, consumer, logger))
                .SetPartitionsAssignedHandler((_, partitions) => OnPartitionsAssigned(partitions, consumer, logger))
                .SetPartitionsRevokedHandler((_, partitions) => OnPartitionsRevoked(partitions, consumer, logger))
                .SetOffsetsCommittedHandler((_, offsets) => OnOffsetsCommitted(offsets, consumer, logger))
                .SetErrorHandler((_, error) => OnError(error, consumer, logger))
                .SetLogHandler((_, logMessage) => OnLog(logMessage, consumer, logger));

        private static void OnStatistics(string statistics, KafkaProducer producer, ISilverbackIntegrationLogger logger)
        {
            logger.LogDebug(KafkaEventIds.ProducerStatisticsReceived, $"Statistics: {statistics}");

            producer.Endpoint.Events.StatisticsHandler?.Invoke(
                KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                statistics,
                producer);
        }

        private static void OnStatistics(string statistics, KafkaConsumer consumer, ISilverbackIntegrationLogger logger)
        {
            logger.LogDebug(KafkaEventIds.ConsumerStatisticsReceived, $"Statistics: {statistics}");

            consumer.Endpoint.Events.StatisticsHandler?.Invoke(
                KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                statistics,
                consumer);
        }

        private static IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
            List<TopicPartition> partitions,
            KafkaConsumer consumer,
            ISilverbackIntegrationLogger logger)
        {
            partitions.ForEach(
                partition =>
                {
                    logger.LogInformation(
                        KafkaEventIds.PartitionsAssigned,
                        "Assigned partition {topic}[{partition}], member id: {memberId}",
                        partition.Topic,
                        partition.Partition.Value,
                        consumer.MemberId);
                });

            var topicPartitionOffsets =
                consumer.Endpoint.Events.PartitionsAssignedHandler?.Invoke(partitions, consumer).ToList() ??
                partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset)).ToList();

            foreach (var topicPartitionOffset in topicPartitionOffsets)
            {
                if (topicPartitionOffset.Offset != Offset.Unset)
                {
                    logger.LogDebug(
                        KafkaEventIds.PartitionOffsetReset,
                        "{topic}[{partition}] offset will be reset to {offset}.",
                        topicPartitionOffset.Topic,
                        topicPartitionOffset.Partition.Value,
                        topicPartitionOffset.Offset);
                }
            }

            consumer.OnPartitionsAssigned(
                topicPartitionOffsets.Select(
                    topicPartitionOffset =>
                        topicPartitionOffset.TopicPartition).ToList());

            return topicPartitionOffsets;
        }

        private static void OnPartitionsRevoked(
            List<TopicPartitionOffset> partitions,
            KafkaConsumer consumer,
            ISilverbackIntegrationLogger logger)
        {
            consumer.OnPartitionsRevoked();

            partitions.ForEach(
                partition =>
                {
                    logger.LogInformation(
                        KafkaEventIds.PartitionsRevoked,
                        "Revoked partition {topic}[{partition}], member id: {memberId}",
                        partition.Topic,
                        partition.Partition.Value,
                        consumer.MemberId);
                });

            consumer.Endpoint.Events.PartitionsRevokedHandler?.Invoke(partitions, consumer);
        }

        private static void OnOffsetsCommitted(
            CommittedOffsets offsets,
            KafkaConsumer consumer,
            ISilverbackIntegrationLogger logger)
        {
            foreach (var offset in offsets.Offsets)
            {
                if (offset.Offset == Offset.Unset)
                    continue;

                if (offset.Error != null && offset.Error.Code != ErrorCode.NoError)
                {
                    logger.LogError(
                        KafkaEventIds.KafkaEventsHandlerErrorWhileCommittingOffset,
                        "Error occurred committing the offset {topic}[{partition}] @{offset}: {errorCode} - {errorReason}",
                        offset.Topic,
                        offset.Partition.Value,
                        offset.Offset,
                        offset.Error.Code,
                        offset.Error.Reason);
                }
                else
                {
                    logger.LogDebug(
                        KafkaEventIds.OffsetCommitted,
                        "Successfully committed offset {topic}[{partition}] @{offset}",
                        offset.Topic,
                        offset.Partition.Value,
                        offset.Offset);
                }
            }

            consumer.Endpoint.Events.OffsetsCommittedHandler?.Invoke(offsets, consumer);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private static void OnError(Error error, KafkaConsumer consumer, ISilverbackIntegrationLogger logger)
        {
            // Ignore errors if not consuming anymore
            // (lidrdkafka randomly throws some "brokers are down" while disconnecting)
            if (!consumer.IsConnected)
                return;

            try
            {
                if (consumer.Endpoint.Events.ErrorHandler != null &&
                    consumer.Endpoint.Events.ErrorHandler.Invoke(error, consumer))
                    return;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    KafkaEventIds.KafkaErrorHandlerError,
                    ex,
                    "Error in Kafka consumer error handler.");
            }

            logger.Log(
                error.IsFatal ? LogLevel.Critical : LogLevel.Error,
                KafkaEventIds.ConsumerError,
                "Error in Kafka consumer ({consumerId}): {error} (topic(s): {topics})",
                consumer.Id,
                error,
                consumer.Endpoint.Names);
        }

        private static void OnLog(LogMessage logMessage, KafkaProducer producer, ISilverbackIntegrationLogger logger) =>
            logger.Log(
                logMessage.Level.ToLogLevel(),
                KafkaEventIds.ConfluentKafkaProducerLog,
                $"Log message from Confluent.Kafka producer ({{endpointName}}) -> {logMessage.Message}",
                producer.Endpoint.Name);

        private static void OnLog(LogMessage logMessage, KafkaConsumer consumer, ISilverbackIntegrationLogger logger) =>
            logger.Log(
                logMessage.Level.ToLogLevel(),
                KafkaEventIds.ConfluentKafkaConsumerLog,
                $"Log message from Confluent.Kafka consumer ({{consumerId}}) -> {logMessage.Message}",
                consumer.Id);
    }
}
