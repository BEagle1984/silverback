// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Diagnostics;

// TODO: Move something to ConsumerLogger or ProducerLogger (enriched)?
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

    private static readonly Action<ILogger, string, int, string, Exception?> ProduceNotAcknowledged =
        SilverbackLoggerMessage.Define<string, int, string>(KafkaLogEvents.ProduceNotAcknowledged);

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

    private static readonly Action<ILogger, string, int, long, string, int, string, Exception?> OffsetCommitError =
        SilverbackLoggerMessage.Define<string, int, long, string, int, string>(KafkaLogEvents.OffsetCommitError);

    private static readonly Action<ILogger, string, int, string, Exception?> ConfluentConsumerError =
        SilverbackLoggerMessage.Define<string, int, string>(KafkaLogEvents.ConfluentConsumerError);

    private static readonly Action<ILogger, string, int, string, Exception?> ConfluentConsumerFatalError =
        SilverbackLoggerMessage.Define<string, int, string>(KafkaLogEvents.ConfluentConsumerFatalError);

    private static readonly Action<ILogger, string, string, Exception?> ConsumerStatisticsReceived =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ConsumerStatisticsReceived);

    private static readonly Action<ILogger, string, string, Exception?> ProducerStatisticsReceived =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ProducerStatisticsReceived);

    private static readonly Action<ILogger, Exception?> StatisticsDeserializationError =
        SilverbackLoggerMessage.Define(KafkaLogEvents.StatisticsDeserializationError);

    private static readonly Action<ILogger, string, string, string, Exception?> PollTimeoutAutoRecovery =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.PollTimeoutAutoRecovery);

    private static readonly Action<ILogger, string, string, string, Exception?> PollTimeoutNoAutoRecovery =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.PollTimeoutNoAutoRecovery);

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

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentProducerLogCritical =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentProducerLogCritical);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentProducerLogError =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentProducerLogError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentProducerLogWarning =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentProducerLogWarning);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentProducerLogInformation =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentProducerLogInformation);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentProducerLogDebug =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentProducerLogDebug);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentConsumerLogCritical =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentConsumerLogCritical);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentConsumerLogError =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentConsumerLogError);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentConsumerLogWarning =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentConsumerLogWarning);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentConsumerLogInformation =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentConsumerLogInformation);

    private static readonly Action<ILogger, string, string, string, Exception?> ConfluentConsumerLogDebug =
        SilverbackLoggerMessage.Define<string, string, string>(KafkaLogEvents.ConfluentConsumerLogDebug);

    private static readonly Action<ILogger, string, int, Exception?> ConfluentAdminClientError =
        SilverbackLoggerMessage.Define<string, int>(KafkaLogEvents.ConfluentAdminClientError);

    private static readonly Action<ILogger, string, int, Exception?> ConfluentAdminClientFatalError =
        SilverbackLoggerMessage.Define<string, int>(KafkaLogEvents.ConfluentAdminClientFatalError);

    private static readonly Action<ILogger, string, string, Exception?> ConfluentAdminClientLogCritical =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ConfluentAdminClientLogCritical);

    private static readonly Action<ILogger, string, string, Exception?> ConfluentAdminClientLogError =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ConfluentAdminClientLogError);

    private static readonly Action<ILogger, string, string, Exception?> ConfluentAdminClientLogWarning =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ConfluentAdminClientLogWarning);

    private static readonly Action<ILogger, string, string, Exception?> ConfluentAdminClientLogInformation =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ConfluentAdminClientLogInformation);

    private static readonly Action<ILogger, string, string, Exception?> ConfluentAdminClientLogDebug =
        SilverbackLoggerMessage.Define<string, string>(KafkaLogEvents.ConfluentAdminClientLogDebug);

    public static void LogConsuming(this ISilverbackLogger logger, ConsumeResult<byte[]?, byte[]?> consumeResult, KafkaConsumer consumer) =>
        ConsumingMessage(logger.InnerLogger, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumer.DisplayName, null);

    public static void LogEndOfPartition(this ISilverbackLogger logger, ConsumeResult<byte[]?, byte[]?> consumeResult, KafkaConsumer consumer) =>
        EndOfPartition(logger.InnerLogger, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumer.DisplayName, null);

    public static void LogKafkaExceptionAutoRecovery(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        KafkaExceptionAutoRecovery(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogKafkaExceptionNoAutoRecovery(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        KafkaExceptionNoAutoRecovery(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogConsumingCanceled(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        ConsumingCanceled(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogProduceNotAcknowledged(this ISilverbackLogger logger, KafkaProducer producer, TopicPartition topicPartition) =>
        ProduceNotAcknowledged(logger.InnerLogger, topicPartition.Topic, topicPartition.Partition.Value, producer.DisplayName, null);

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

    public static void LogOffsetCommitError(this ISilverbackLogger logger, TopicPartitionOffsetError topicPartitionOffsetError, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.OffsetCommitError))
            return;

        OffsetCommitError(
            logger.InnerLogger,
            topicPartitionOffsetError.Topic,
            topicPartitionOffsetError.Partition,
            topicPartitionOffsetError.Offset,
            GetErrorReason(topicPartitionOffsetError.Error),
            (int)topicPartitionOffsetError.Error.Code,
            consumer.DisplayName,
            null);
    }

    public static void LogConfluentConsumerError(this ISilverbackLogger logger, Error error, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerError))
            return;

        ConfluentConsumerError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            consumer.DisplayName,
            null);
    }

    public static void LogConfluentConsumerFatalError(this ISilverbackLogger logger, Error error, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerFatalError))
            return;

        ConfluentConsumerFatalError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            consumer.DisplayName,
            null);
    }

    public static void LogConsumerStatisticsReceived(this ISilverbackLogger logger, string statisticsJson, KafkaConsumer consumer) =>
        ConsumerStatisticsReceived(logger.InnerLogger, statisticsJson, consumer.DisplayName, null);

    public static void LogProducerStatisticsReceived(this ISilverbackLogger logger, string statisticsJson, IConfluentProducerWrapper producerWrapper) =>
        ProducerStatisticsReceived(logger.InnerLogger, statisticsJson, producerWrapper.DisplayName, null);

    public static void LogStatisticsDeserializationError(this ISilverbackLogger logger, Exception exception) =>
        StatisticsDeserializationError(logger.InnerLogger, exception);

    public static void LogPollTimeoutAutoRecovery(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.PollTimeoutAutoRecovery))
            return;

        PollTimeoutAutoRecovery(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogPollTimeoutNoAutoRecovery(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.PollTimeoutNoAutoRecovery))
            return;

        PollTimeoutNoAutoRecovery(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogTransactionsInitialized(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionsInitialized(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogTransactionStarted(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionStarted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogTransactionCommitted(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionCommitted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogTransactionAborted(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionAborted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId, null);

    public static void LogOffsetSentToTransaction(
        this ISilverbackLogger logger,
        IConfluentProducerWrapper producerWrapper,
        TopicPartitionOffset topicPartitionOffset) =>
        OffsetSentToTransaction(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            producerWrapper.DisplayName,
            producerWrapper.Configuration.TransactionalId,
            null);

    public static void LogConfluentProducerLogCritical(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogCritical))
            return;

        ConfluentProducerLogCritical(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName, null);
    }

    public static void LogConfluentProducerLogError(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogError))
            return;

        ConfluentProducerLogError(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName, null);
    }

    public static void LogConfluentProducerLogWarning(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogWarning))
            return;

        ConfluentProducerLogWarning(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName, null);
    }

    public static void LogConfluentProducerLogInformation(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogInformation))
            return;

        ConfluentProducerLogInformation(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName, null);
    }

    public static void LogConfluentProducerLogDebug(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogDebug))
            return;

        ConfluentProducerLogDebug(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName, null);
    }

    public static void LogConfluentConsumerLogCritical(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogCritical))
            return;

        ConfluentConsumerLogCritical(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogConfluentConsumerLogError(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogError))
            return;

        ConfluentConsumerLogError(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogConfluentConsumerLogWarning(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogWarning))
            return;

        ConfluentConsumerLogWarning(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogConfluentConsumerLogInformation(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogInformation))
            return;

        ConfluentConsumerLogInformation(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogConfluentConsumerLogDebug(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogDebug))
            return;

        ConfluentConsumerLogDebug(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName, null);
    }

    public static void LogConfluentAdminClientError(this ISilverbackLogger logger, Error error)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientError))
            return;

        ConfluentAdminClientError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            null);
    }

    public static void LogConfluentAdminClientFatalError(this ISilverbackLogger logger, Error error)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientFatalError))
            return;

        ConfluentAdminClientFatalError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            null);
    }

    public static void LogConfluentAdminClientLogCritical(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogCritical))
            return;

        ConfluentAdminClientLogCritical(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, null);
    }

    public static void LogConfluentAdminClientLogError(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogError))
            return;

        ConfluentAdminClientLogError(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, null);
    }

    public static void LogConfluentAdminClientLogWarning(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogWarning))
            return;

        ConfluentAdminClientLogWarning(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, null);
    }

    public static void LogConfluentAdminClientLogInformation(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogInformation))
            return;

        ConfluentAdminClientLogInformation(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, null);
    }

    public static void LogConfluentAdminClientLogDebug(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogDebug))
            return;

        ConfluentAdminClientLogDebug(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, null);
    }

    private static string GetErrorReason(Error error) =>
        !string.IsNullOrEmpty(error.Reason) ? error.Reason : error.Code.GetReason();
}
