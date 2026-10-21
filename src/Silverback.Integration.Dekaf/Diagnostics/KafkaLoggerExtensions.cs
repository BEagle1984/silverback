// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Diagnostics;

internal static class KafkaLoggerExtensions
{
    private static readonly Action<ILogger, string, int, long, string, Exception?> ConsumingMessage =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.ConsumingMessage);

    private static readonly Action<ILogger, string, int, long, string, Exception?> EndOfPartition =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.EndOfPartition);

    private static readonly Action<ILogger, string, Exception?> KafkaExceptionAutoRecovery =
        SilverbackLoggerMessage.Define<string>(KafkaLogEvents.KafkaExceptionAutoRecovery);

    private static readonly Action<ILogger, string, Exception?> KafkaExceptionNoAutoRecovery =
        SilverbackLoggerMessage.Define<string>(KafkaLogEvents.KafkaExceptionNoAutoRecovery);

    private static readonly Action<ILogger, string, Exception?> ConsumingCanceled =
        SilverbackLoggerMessage.Define<string>(KafkaLogEvents.ConsumingCanceled);

    private static readonly Action<ILogger, TimeSpan, string, Exception?> StaleConsumer =
        SilverbackLoggerMessage.Define<TimeSpan, string>(KafkaLogEvents.StaleConsumer);

    private static readonly Action<ILogger, string, int, long, string, Exception?> PartitionStaticallyAssigned =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.PartitionStaticallyAssigned);

    private static readonly Action<ILogger, string, int, string, Exception?> PartitionAssigned =
        SilverbackLoggerMessage.Define<string, int, string>(KafkaLogEvents.PartitionAssigned);

    private static readonly Action<ILogger, string, int, long, string, Exception?> PartitionOffsetReset =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.PartitionOffsetReset);

    private static readonly Action<ILogger, string, int, long, string, Exception?> PartitionRevoked =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.PartitionRevoked);

    private static readonly Action<ILogger, string, int, long, string, Exception?> PartitionPaused =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.PartitionPaused);

    private static readonly Action<ILogger, string, int, string, Exception?> PartitionResumed =
        SilverbackLoggerMessage.Define<string, int, string>(KafkaLogEvents.PartitionResumed);

    private static readonly Action<ILogger, string, int, long, string, Exception?> OffsetCommitted =
        SilverbackLoggerMessage.Define<string, int, long, string>(KafkaLogEvents.OffsetCommitted);

    private static readonly Action<ILogger, string, string?, Exception?> TransactionsInitialized =
        SilverbackLoggerMessage.Define<string, string?>(KafkaLogEvents.TransactionsInitialized);

    private static readonly Action<ILogger, string, string?, Exception?> TransactionStarted =
        SilverbackLoggerMessage.Define<string, string?>(KafkaLogEvents.TransactionStarted);

    private static readonly Action<ILogger, string, string?, Exception?> TransactionCommitted =
        SilverbackLoggerMessage.Define<string, string?>(KafkaLogEvents.TransactionCommitted);

    private static readonly Action<ILogger, string, string?, Exception?> TransactionAborted =
        SilverbackLoggerMessage.Define<string, string?>(KafkaLogEvents.TransactionAborted);

    private static readonly Action<ILogger, string, int, long, string, string?, Exception?> OffsetSentToTransaction =
        SilverbackLoggerMessage.Define<string, int, long, string, string?>(KafkaLogEvents.OffsetSentToTransaction);

    public static void LogConsuming(this ISilverbackLogger logger, KafkaConsumedRecord consumedRecord, KafkaConsumer consumer) =>
        ConsumingMessage(logger.InnerLogger, consumedRecord.Topic, consumedRecord.Partition, consumedRecord.Offset, consumer.DisplayName, null);

    public static void LogEndOfPartition(this ISilverbackLogger logger, KafkaConsumedRecord consumedRecord, KafkaConsumer consumer) =>
        EndOfPartition(logger.InnerLogger, consumedRecord.Topic, consumedRecord.Partition, consumedRecord.Offset, consumer.DisplayName, null);

    public static void LogKafkaExceptionAutoRecovery(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        KafkaExceptionAutoRecovery(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogKafkaExceptionNoAutoRecovery(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        KafkaExceptionNoAutoRecovery(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogConsumingCanceled(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        ConsumingCanceled(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogStaleConsumer(this ISilverbackLogger logger, TimeSpan stallDetectionThreshold, KafkaConsumer consumer) =>
        StaleConsumer(logger.InnerLogger, stallDetectionThreshold, consumer.DisplayName, null);

    public static void LogPartitionStaticallyAssigned(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionStaticallyAssigned(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName,
            null);

    public static void LogPartitionAssigned(this ISilverbackLogger logger, TopicPartition topicPartition, KafkaConsumer consumer) =>
        PartitionAssigned(logger.InnerLogger, topicPartition.Topic, topicPartition.Partition, consumer.DisplayName, null);

    public static void LogPartitionOffsetReset(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionOffsetReset(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName,
            null);

    public static void LogPartitionRevoked(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionRevoked(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName,
            null);

    public static void LogPartitionPaused(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionPaused(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName,
            null);

    public static void LogPartitionResumed(this ISilverbackLogger logger, TopicPartition topicPartition, KafkaConsumer consumer) =>
        PartitionResumed(
            logger.InnerLogger,
            topicPartition.Topic,
            topicPartition.Partition,
            consumer.DisplayName,
            null);

    public static void LogOffsetCommitted(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        OffsetCommitted(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName,
            null);

    public static void LogTransactionsInitialized(this ISilverbackLogger logger, IDekafProducerWrapper producerWrapper) =>
        TransactionsInitialized(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogTransactionStarted(this ISilverbackLogger logger, IDekafProducerWrapper producerWrapper) =>
        TransactionStarted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogTransactionCommitted(this ISilverbackLogger logger, IDekafProducerWrapper producerWrapper) =>
        TransactionCommitted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogTransactionAborted(this ISilverbackLogger logger, IDekafProducerWrapper producerWrapper) =>
        TransactionAborted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogOffsetSentToTransaction(
        this ISilverbackLogger logger,
        IDekafProducerWrapper producerWrapper,
        TopicPartitionOffset topicPartitionOffset) =>
        OffsetSentToTransaction(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            producerWrapper.DisplayName,
            producerWrapper.Configuration.TransactionalId,
            null);
}
