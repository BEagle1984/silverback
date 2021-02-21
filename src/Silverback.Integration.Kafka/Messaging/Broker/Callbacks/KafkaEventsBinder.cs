// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks.Statistics;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Messaging.Broker.Callbacks
{
    internal static class KafkaEventsBinder
    {
        public static void SetEventsHandlers(
            this IConfluentProducerBuilder producerBuilder,
            KafkaProducer producer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger) =>
            producerBuilder
                .SetStatisticsHandler(
                    (_, statistics) => OnStatistics(statistics, producer, callbacksInvoker, logger))
                .SetLogHandler((_, logMessage) => OnLog(logMessage, producer, logger));

        public static void SetEventsHandlers(
            this IConfluentConsumerBuilder consumerBuilder,
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger) =>
            consumerBuilder
                .SetStatisticsHandler(
                    (_, statistics) => OnStatistics(statistics, consumer, callbacksInvoker, logger))
                .SetPartitionsAssignedHandler(
                    (_, partitions) => OnPartitionsAssigned(
                        partitions,
                        consumer,
                        callbacksInvoker,
                        logger))
                .SetPartitionsRevokedHandler(
                    (_, partitions) => OnPartitionsRevoked(
                        partitions,
                        consumer,
                        callbacksInvoker,
                        logger))
                .SetOffsetsCommittedHandler(
                    (_, offsets) => OnOffsetsCommitted(offsets, consumer, callbacksInvoker, logger))
                .SetErrorHandler((_, error) => OnError(error, consumer, callbacksInvoker, logger))
                .SetLogHandler((_, logMessage) => OnLog(logMessage, consumer, logger));

        private static void OnStatistics(
            string statistics,
            KafkaProducer producer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            logger.LogProducerStatisticsReceived(statistics, producer);

            callbacksInvoker.Invoke<IKafkaProducerStatisticsCallback>(
                handler => handler.OnProducerStatistics(
                    KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                    statistics,
                    producer));
        }

        private static void OnStatistics(
            string statistics,
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            logger.LogConsumerStatisticsReceived(statistics, consumer);

            callbacksInvoker.Invoke<IKafkaConsumerStatisticsCallback>(
                handler => handler.OnConsumerStatistics(
                    KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                    statistics,
                    consumer));
        }

        [SuppressMessage(
            "",
            "CA1508",
            Justification = "False positive: topicPartitionOffsets set in handler action")]
        private static IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
            List<TopicPartition> partitions,
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            partitions.ForEach(topicPartition => logger.LogPartitionAssigned(topicPartition, consumer));

            List<TopicPartitionOffset>? topicPartitionOffsets = null;

            callbacksInvoker.Invoke<IKafkaPartitionsAssignedCallback>(
                handler =>
                {
                    var result = handler.OnPartitionsAssigned(partitions, consumer);

                    if (result != null)
                        topicPartitionOffsets = result.ToList();
                });

            if (topicPartitionOffsets == null)
            {
                topicPartitionOffsets = partitions
                    .Select(partition => new TopicPartitionOffset(partition, Offset.Unset))
                    .ToList();
            }

            foreach (var topicPartitionOffset in topicPartitionOffsets)
            {
                if (topicPartitionOffset.Offset != Offset.Unset)
                    logger.LogPartitionOffsetReset(topicPartitionOffset, consumer);
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
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            consumer.OnPartitionsRevoked();

            partitions.ForEach(
                topicPartitionOffset => logger.LogPartitionRevoked(topicPartitionOffset, consumer));

            callbacksInvoker.Invoke<IKafkaPartitionsRevokedCallback>(
                handler => handler.OnPartitionsRevoked(partitions, consumer));
        }

        private static void OnOffsetsCommitted(
            CommittedOffsets offsets,
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            foreach (var topicPartitionOffsetError in offsets.Offsets)
            {
                if (topicPartitionOffsetError.Offset == Offset.Unset)
                    continue;

                if (topicPartitionOffsetError.Error != null &&
                    topicPartitionOffsetError.Error.Code != ErrorCode.NoError)
                {
                    logger.LogOffsetCommitError(topicPartitionOffsetError, consumer);
                }
                else
                {
                    logger.LogOffsetCommitted(topicPartitionOffsetError.TopicPartitionOffset, consumer);
                }
            }

            callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(
                handler => handler.OnOffsetsCommitted(offsets, consumer));
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private static void OnError(
            Error error,
            KafkaConsumer consumer,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger logger)
        {
            // Ignore errors if not consuming anymore
            // (lidrdkafka randomly throws some "brokers are down" while disconnecting)
            if (!consumer.IsConnected)
                return;

            try
            {
                bool handled = false;

                callbacksInvoker.Invoke<IKafkaConsumerErrorCallback>(
                    handler => { handled &= handler.OnConsumerError(error, consumer); });

                if (handled)
                    return;
            }
            catch (Exception ex)
            {
                logger.LogKafkaErrorHandlerError(consumer, ex);
            }

            if (error.IsFatal)
                logger.LogConfluentConsumerFatalError(error, consumer);
            else
                logger.LogConfluentConsumerError(error, consumer);
        }

        private static void OnLog(
            LogMessage logMessage,
            KafkaProducer producer,
            ISilverbackLogger logger)
        {
            switch (logMessage.Level)
            {
                case SyslogLevel.Emergency:
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                    logger.LogConfluentProducerLogCritical(logMessage, producer);
                    break;
                case SyslogLevel.Error:
                    logger.LogConfluentProducerLogError(logMessage, producer);
                    break;
                case SyslogLevel.Warning:
                    logger.LogConfluentProducerLogWarning(logMessage, producer);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    logger.LogConfluentProducerLogInformation(logMessage, producer);
                    break;
                default:
                    logger.LogConfluentProducerLogDebug(logMessage, producer);
                    break;
            }
        }

        private static void OnLog(
            LogMessage logMessage,
            KafkaConsumer consumer,
            ISilverbackLogger logger)
        {
            switch (logMessage.Level)
            {
                case SyslogLevel.Emergency:
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                    logger.LogConfluentConsumerLogCritical(logMessage, consumer);
                    break;
                case SyslogLevel.Error:
                    logger.LogConfluentConsumerLogError(logMessage, consumer);
                    break;
                case SyslogLevel.Warning:
                    logger.LogConfluentConsumerLogWarning(logMessage, consumer);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    logger.LogConfluentConsumerLogInformation(logMessage, consumer);
                    break;
                default:
                    logger.LogConfluentConsumerLogDebug(logMessage, consumer);
                    break;
            }
        }
    }
}
