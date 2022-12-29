// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks.Statistics;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

internal static class KafkaEventsBinder
{
    public static IConfluentProducerBuilder SetEventsHandlers(
        this IConfluentProducerBuilder producerBuilder,
        IConfluentProducerWrapper producerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger) =>
        producerBuilder
            .SetStatisticsHandler((_, statistics) => OnStatistics(statistics, producerWrapper, callbacksInvoker, logger))
            .SetLogHandler((_, logMessage) => OnLog(logMessage, producerWrapper, callbacksInvoker, logger));

    public static IConfluentConsumerBuilder SetEventsHandlers(
        this IConfluentConsumerBuilder consumerBuilder,
        IConfluentConsumerWrapper consumerWrapper,
        KafkaConsumerConfiguration consumerConfiguration,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        consumerBuilder
            .SetStatisticsHandler((_, statistics) => OnStatistics(statistics, consumerWrapper, callbacksInvoker, logger))
            .SetPartitionsAssignedHandler(
                (_, topicPartitions) => OnPartitionsAssigned(
                    topicPartitions,
                    consumerWrapper,
                    callbacksInvoker,
                    logger))
            .SetOffsetsCommittedHandler((_, offsets) => OnOffsetsCommitted(offsets, consumerWrapper, callbacksInvoker, logger))
            .SetErrorHandler((_, error) => OnError(error, consumerWrapper, callbacksInvoker, logger))
            .SetLogHandler((_, logMessage) => OnLog(logMessage, consumerWrapper, callbacksInvoker, logger));

        if (consumerConfiguration.PartitionAssignmentStrategy != null &&
            consumerConfiguration.PartitionAssignmentStrategy.Value.HasFlag(PartitionAssignmentStrategy.CooperativeSticky))
        {
            // This is done to discard the result of the revoke handler. Using the SetPartitionsRevokedHandler
            // overload with a Func<> results in an exception in the internal rebalance method since you are
            // not supposed to alter the partitions being revoked with the cooperative sticky strategy.
            consumerBuilder
                .SetPartitionsRevokedHandler(
                    (_, topicPartitionOffsets) =>
                    {
                        OnPartitionsRevoked(topicPartitionOffsets, consumerWrapper, callbacksInvoker, logger);
                    });
        }
        else
        {
            consumerBuilder
                .SetPartitionsRevokedHandler(
                    (_, topicPartitionOffsets) =>
                        OnPartitionsRevoked(topicPartitionOffsets, consumerWrapper, callbacksInvoker, logger));
        }

        return consumerBuilder;
    }

    private static void OnStatistics(
        string statistics,
        IConfluentProducerWrapper producerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        logger.LogProducerStatisticsReceived(statistics, producerWrapper);

        callbacksInvoker.Invoke<IKafkaProducerStatisticsCallback>(
            callback => callback.OnProducerStatistics(
                KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                statistics,
                producerWrapper),
            invokeDuringShutdown: false);
    }

    private static void OnStatistics(
        string statistics,
        IConfluentConsumerWrapper consumerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        logger.LogConsumerStatisticsReceived(statistics, consumerWrapper.Consumer);

        callbacksInvoker.Invoke<IKafkaConsumerStatisticsCallback>(
            callback => callback.OnConsumerStatistics(
                KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                statistics,
                consumerWrapper.Consumer),
            invokeDuringShutdown: false);
    }

    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive: topicPartitionOffsets set in handler action")]
    private static IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
        List<TopicPartition> topicPartitions,
        IConfluentConsumerWrapper consumerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        topicPartitions.ForEach(topicPartition => logger.LogPartitionAssigned(topicPartition, consumerWrapper.Consumer));

        List<TopicPartitionOffset>? topicPartitionOffsets = null;

        callbacksInvoker.Invoke<IKafkaPartitionsAssignedCallback>(
            callback =>
            {
                IEnumerable<TopicPartitionOffset>? result = callback.OnPartitionsAssigned(topicPartitions, consumerWrapper.Consumer);

                if (result != null)
                    topicPartitionOffsets = result.ToList();
            });

        topicPartitionOffsets ??= topicPartitions
            .Select(partition => new TopicPartitionOffset(partition, Offset.Unset))
            .ToList();

        foreach (TopicPartitionOffset? topicPartitionOffset in topicPartitionOffsets)
        {
            if (topicPartitionOffset.Offset != Offset.Unset)
                logger.LogPartitionOffsetReset(topicPartitionOffset, consumerWrapper.Consumer);
        }

        return consumerWrapper.Consumer.OnPartitionsAssigned(topicPartitionOffsets);
    }

    private static void OnPartitionsRevoked(
        List<TopicPartitionOffset> topicPartitionOffsets,
        IConfluentConsumerWrapper consumerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        consumerWrapper.Consumer.OnPartitionsRevoked(topicPartitionOffsets);

        topicPartitionOffsets.ForEach(topicPartitionOffset => logger.LogPartitionRevoked(topicPartitionOffset, consumerWrapper.Consumer));

        callbacksInvoker.Invoke<IKafkaPartitionsRevokedCallback>(
            callback =>
                callback.OnPartitionsRevoked(topicPartitionOffsets, consumerWrapper.Consumer));
    }

    private static void OnOffsetsCommitted(
        CommittedOffsets offsets,
        IConfluentConsumerWrapper consumerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        foreach (TopicPartitionOffsetError? topicPartitionOffsetError in offsets.Offsets)
        {
            if (topicPartitionOffsetError.Offset == Offset.Unset)
                continue;

            if (topicPartitionOffsetError.Error != null &&
                topicPartitionOffsetError.Error.Code != ErrorCode.NoError)
            {
                logger.LogOffsetCommitError(topicPartitionOffsetError, consumerWrapper.Consumer);
            }
            else
            {
                logger.LogOffsetCommitted(topicPartitionOffsetError.TopicPartitionOffset, consumerWrapper.Consumer);
            }
        }

        callbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(callback => callback.OnOffsetsCommitted(offsets, consumerWrapper.Consumer));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private static void OnError(
        Error error,
        IConfluentConsumerWrapper consumerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        // Ignore errors if not consuming anymore
        // (lidrdkafka randomly throws some "brokers are down" while disconnecting)
        if (consumerWrapper.Status is not (ClientStatus.Initialized or ClientStatus.Initializing))
            return;

        bool handled = false;

        callbacksInvoker.Invoke<IKafkaConsumerErrorCallback>(
            handler =>
            {
                handled &= handler.OnConsumerError(error, consumerWrapper.Consumer);
            });

        if (handled)
            return;

        if (error.IsFatal)
            logger.LogConfluentConsumerFatalError(error, consumerWrapper.Consumer);
        else
            logger.LogConfluentConsumerError(error, consumerWrapper.Consumer);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private static void OnLog(
        LogMessage logMessage,
        IConfluentProducerWrapper producerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        bool handled = false;

        callbacksInvoker.Invoke<IKafkaProducerLogCallback>(
            handler =>
            {
                handled &= handler.OnProducerLog(logMessage, producerWrapper);
            });

        if (handled)
            return;

        switch (logMessage.Level)
        {
            case SyslogLevel.Emergency:
            case SyslogLevel.Alert:
            case SyslogLevel.Critical:
                logger.LogConfluentProducerLogCritical(logMessage, producerWrapper);
                break;
            case SyslogLevel.Error:
                logger.LogConfluentProducerLogError(logMessage, producerWrapper);
                break;
            case SyslogLevel.Warning:
                logger.LogConfluentProducerLogWarning(logMessage, producerWrapper);
                break;
            case SyslogLevel.Notice:
            case SyslogLevel.Info:
                logger.LogConfluentProducerLogInformation(logMessage, producerWrapper);
                break;
            default:
                logger.LogConfluentProducerLogDebug(logMessage, producerWrapper);
                break;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private static void OnLog(
        LogMessage logMessage,
        IConfluentConsumerWrapper consumerWrapper,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        ISilverbackLogger logger)
    {
        bool handled = false;

        callbacksInvoker.Invoke<IKafkaConsumerLogCallback>(
            handler =>
            {
                handled &= handler.OnConsumerLog(logMessage, consumerWrapper.Consumer);
            });

        if (handled)
            return;

        switch (logMessage.Level)
        {
            case SyslogLevel.Emergency:
            case SyslogLevel.Alert:
            case SyslogLevel.Critical:
                logger.LogConfluentConsumerLogCritical(logMessage, consumerWrapper.Consumer);
                break;
            case SyslogLevel.Error:
                logger.LogConfluentConsumerLogError(logMessage, consumerWrapper.Consumer);
                break;
            case SyslogLevel.Warning:
                logger.LogConfluentConsumerLogWarning(logMessage, consumerWrapper.Consumer);
                break;
            case SyslogLevel.Notice:
            case SyslogLevel.Info:
                logger.LogConfluentConsumerLogInformation(logMessage, consumerWrapper.Consumer);
                break;
            default:
                logger.LogConfluentConsumerLogDebug(logMessage, consumerWrapper.Consumer);
                break;
        }
    }
}
