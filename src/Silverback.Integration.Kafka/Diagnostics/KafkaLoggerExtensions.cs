// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;

namespace Silverback.Diagnostics;

// The KafkaLogEvents properties are generated from the [LoggerMessage] methods, including their documentation
internal static partial class KafkaLoggerExtensions
{
    public static void LogConsuming(this ISilverbackLogger logger, ConsumeResult<byte[]?, byte[]?> consumeResult, KafkaConsumer consumer) =>
        ConsumingMessage(logger.InnerLogger, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumer.DisplayName);

    public static void LogEndOfPartition(this ISilverbackLogger logger, ConsumeResult<byte[]?, byte[]?> consumeResult, KafkaConsumer consumer) =>
        EndOfPartition(logger.InnerLogger, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumer.DisplayName);

    public static void LogKafkaExceptionAutoRecovery(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        KafkaExceptionAutoRecovery(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogKafkaExceptionNoAutoRecovery(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        KafkaExceptionNoAutoRecovery(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogConsumingCanceled(this ISilverbackLogger logger, KafkaConsumer consumer, Exception exception) =>
        ConsumingCanceled(logger.InnerLogger, consumer.DisplayName, exception);

    public static void LogStaleConsumer(this ISilverbackLogger logger, TimeSpan stallDetectionThreshold, KafkaConsumer consumer) =>
        StaleConsumer(logger.InnerLogger, stallDetectionThreshold, consumer.DisplayName);

    public static void LogProduceNotAcknowledged(this ISilverbackLogger logger, KafkaProducer producer, TopicPartition topicPartition) =>
        ProduceNotAcknowledged(logger.InnerLogger, topicPartition.Topic, topicPartition.Partition.Value, producer.DisplayName);

    public static void LogPartitionStaticallyAssigned(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionStaticallyAssigned(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName);

    public static void LogPartitionAssigned(this ISilverbackLogger logger, TopicPartition topicPartition, KafkaConsumer consumer) =>
        PartitionAssigned(logger.InnerLogger, topicPartition.Topic, topicPartition.Partition, consumer.DisplayName);

    public static void LogPartitionOffsetReset(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionOffsetReset(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName);

    public static void LogPartitionRevoked(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionRevoked(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName);

    public static void LogPartitionPaused(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        PartitionPaused(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName);

    public static void LogPartitionResumed(this ISilverbackLogger logger, TopicPartition topicPartition, KafkaConsumer consumer) =>
        PartitionResumed(
            logger.InnerLogger,
            topicPartition.Topic,
            topicPartition.Partition,
            consumer.DisplayName);

    public static void LogOffsetCommitted(this ISilverbackLogger logger, TopicPartitionOffset topicPartitionOffset, KafkaConsumer consumer) =>
        OffsetCommitted(
            logger.InnerLogger,
            topicPartitionOffset.Topic,
            topicPartitionOffset.Partition,
            topicPartitionOffset.Offset,
            consumer.DisplayName);

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
            consumer.DisplayName);
    }

    public static void LogConfluentConsumerError(this ISilverbackLogger logger, Error error, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerError))
            return;

        ConfluentConsumerError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            consumer.DisplayName);
    }

    public static void LogConfluentConsumerFatalError(this ISilverbackLogger logger, Error error, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerFatalError))
            return;

        ConfluentConsumerFatalError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            consumer.DisplayName);
    }

    public static void LogConsumerStatisticsReceived(this ISilverbackLogger logger, string statisticsJson, KafkaConsumer consumer) =>
        ConsumerStatisticsReceived(logger.InnerLogger, statisticsJson, consumer.DisplayName);

    public static void LogProducerStatisticsReceived(this ISilverbackLogger logger, string statisticsJson, IConfluentProducerWrapper producerWrapper) =>
        ProducerStatisticsReceived(logger.InnerLogger, statisticsJson, producerWrapper.DisplayName);

    public static void LogStatisticsDeserializationError(this ISilverbackLogger logger, Exception exception) =>
        StatisticsDeserializationError(logger.InnerLogger, exception);

    public static void LogPollTimeoutAutoRecovery(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.PollTimeoutAutoRecovery))
            return;

        PollTimeoutAutoRecovery(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogPollTimeoutNoAutoRecovery(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.PollTimeoutNoAutoRecovery))
            return;

        PollTimeoutNoAutoRecovery(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogTransactionsInitialized(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionsInitialized(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId);

    public static void LogTransactionStarted(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionStarted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId);

    public static void LogTransactionCommitted(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionCommitted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId);

    public static void LogTransactionAborted(this ISilverbackLogger logger, IConfluentProducerWrapper producerWrapper) =>
        TransactionAborted(logger.InnerLogger, producerWrapper.DisplayName, producerWrapper.Configuration.TransactionalId);

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
            producerWrapper.Configuration.TransactionalId);

    public static void LogConfluentProducerLogCritical(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogCritical))
            return;

        ConfluentProducerLogCritical(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName);
    }

    public static void LogConfluentProducerError(this ISilverbackLogger logger, Error error, KafkaProducer producer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerError))
            return;

        ConfluentProducerError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            producer.DisplayName);
    }

    public static void LogConfluentProducerFatalError(this ISilverbackLogger logger, Error error, KafkaProducer producer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerFatalError))
            return;

        ConfluentProducerFatalError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code,
            producer.DisplayName);
    }

    public static void LogConfluentProducerLogError(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogError))
            return;

        ConfluentProducerLogError(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName);
    }

    public static void LogConfluentProducerLogWarning(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogWarning))
            return;

        ConfluentProducerLogWarning(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName);
    }

    public static void LogConfluentProducerLogInformation(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogInformation))
            return;

        ConfluentProducerLogInformation(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName);
    }

    public static void LogConfluentProducerLogDebug(this ISilverbackLogger logger, LogMessage logMessage, IConfluentProducerWrapper producerWrapper)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentProducerLogDebug))
            return;

        ConfluentProducerLogDebug(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, producerWrapper.DisplayName);
    }

    public static void LogConfluentConsumerLogCritical(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogCritical))
            return;

        ConfluentConsumerLogCritical(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogConfluentConsumerLogError(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogError))
            return;

        ConfluentConsumerLogError(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogConfluentConsumerLogWarning(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogWarning))
            return;

        ConfluentConsumerLogWarning(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogConfluentConsumerLogInformation(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogInformation))
            return;

        ConfluentConsumerLogInformation(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogConfluentConsumerLogDebug(this ISilverbackLogger logger, LogMessage logMessage, KafkaConsumer consumer)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentConsumerLogDebug))
            return;

        ConfluentConsumerLogDebug(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message, consumer.DisplayName);
    }

    public static void LogConfluentAdminClientError(this ISilverbackLogger logger, Error error)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientError))
            return;

        ConfluentAdminClientError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code);
    }

    public static void LogConfluentAdminClientFatalError(this ISilverbackLogger logger, Error error)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientFatalError))
            return;

        ConfluentAdminClientFatalError(
            logger.InnerLogger,
            GetErrorReason(error),
            (int)error.Code);
    }

    public static void LogConfluentAdminClientLogCritical(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogCritical))
            return;

        ConfluentAdminClientLogCritical(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message);
    }

    public static void LogConfluentAdminClientLogError(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogError))
            return;

        ConfluentAdminClientLogError(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message);
    }

    public static void LogConfluentAdminClientLogWarning(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogWarning))
            return;

        ConfluentAdminClientLogWarning(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message);
    }

    public static void LogConfluentAdminClientLogInformation(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogInformation))
            return;

        ConfluentAdminClientLogInformation(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message);
    }

    public static void LogConfluentAdminClientLogDebug(this ISilverbackLogger logger, LogMessage logMessage)
    {
        if (!logger.IsEnabled(KafkaLogEvents.ConfluentAdminClientLogDebug))
            return;

        ConfluentAdminClientLogDebug(logger.InnerLogger, logMessage.Level.ToString(), logMessage.Message);
    }

    private static string GetErrorReason(Error error) =>
        !string.IsNullOrEmpty(error.Reason) ? error.Reason : error.Code.GetReason();

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is consumed from a
    ///     Kafka topic.
    /// </summary>
    [LoggerMessage(
        2011,
        LogLevel.Debug,
        "Consuming message {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConsumingMessage")]
    private static partial void ConsumingMessage(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the end of partition is
    ///     reached.
    /// </summary>
    [LoggerMessage(
        2012,
        LogLevel.Information,
        "Partition EOF reached: {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_EndOfPartition")]
    private static partial void EndOfPartition(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <see cref="KafkaException" /> is thrown inside the
    ///     <c>Consume</c> method. The consumer will automatically recover from these exceptions (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    [LoggerMessage(
        2013,
        LogLevel.Warning,
        "Error occurred trying to pull next message; the consumer will try to recover | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_KafkaExceptionAutoRecovery")]
    private static partial void KafkaExceptionAutoRecovery(ILogger logger, string consumerName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <see cref="KafkaException" /> is thrown inside the
    ///     <c>Consume</c> method. The consumer will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
    /// </summary>
    [LoggerMessage(
        2014,
        LogLevel.Error,
        "Error occurred trying to pull next message; the consumer will be stopped (auto recovery disabled for consumer)" +
        " | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_KafkaExceptionNoAutoRecovery")]
    private static partial void KafkaExceptionNoAutoRecovery(ILogger logger, string consumerName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the <c>Consume</c> is aborted
    ///     (usually because the broker is being disconnected or the application is exiting).
    /// </summary>
    [LoggerMessage(
        2016,
        LogLevel.Trace,
        "Consuming canceled | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConsumingCanceled")]
    private static partial void ConsumingCanceled(ILogger logger, string consumerName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer did not consume any message within the
    ///     stall detection threshold.
    /// </summary>
    [LoggerMessage(
        2017,
        LogLevel.Information,
        "No message consumed within the stall detection threshold ({StallDetectionThreshold}); the consumer will restart" +
        " | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_StaleConsumer")]
    private static partial void StaleConsumer(ILogger logger, TimeSpan stallDetectionThreshold, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is sent to the
    ///     broker but no acknowledge is received. This is logged only if <c>ThrowIfNotAcknowledged</c> is
    ///     <c>false</c>.
    /// </summary>
    [LoggerMessage(
        2022,
        LogLevel.Warning,
        "Message produced to {Topic}[{Partition}] but not acknowledged | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ProduceNotAcknowledged")]
    private static partial void ProduceNotAcknowledged(ILogger logger, string topic, int partition, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a static partition assignment is set for the consumer.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each assigned partition.
    /// </remarks>
    [LoggerMessage(
        2031,
        LogLevel.Information,
        "Assigned partition {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PartitionStaticallyAssigned")]
    private static partial void PartitionStaticallyAssigned(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer group partition
    ///     assignment has been received by a consumer.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each assigned partition.
    /// </remarks>
    [LoggerMessage(
        2032,
        LogLevel.Information,
        "Assigned partition {Topic}[{Partition}] | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PartitionAssigned")]
    private static partial void PartitionAssigned(ILogger logger, string topic, int partition, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the offset of an assigned
    ///     partition is being reset.
    /// </summary>
    [LoggerMessage(
        2033,
        LogLevel.Debug,
        "{Topic}[{Partition}] offset will be reset to {Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PartitionOffsetReset")]
    private static partial void PartitionOffsetReset(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a group partition assignment
    ///     is being revoked.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each revoked partition.
    /// </remarks>
    [LoggerMessage(
        2034,
        LogLevel.Information,
        "Revoked partition {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PartitionRevoked")]
    private static partial void PartitionRevoked(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a partition is paused.
    /// </summary>
    [LoggerMessage(
        2035,
        LogLevel.Debug,
        "Partition {Topic}[{Partition}] paused at offset {Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PartitionPaused")]
    private static partial void PartitionPaused(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a partition is resumed.
    /// </summary>
    [LoggerMessage(
        2036,
        LogLevel.Debug,
        "Partition {Topic}[{Partition}] resumed | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PartitionResumed")]
    private static partial void PartitionResumed(ILogger logger, string topic, int partition, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an offset is successfully committed.
    /// </summary>
    [LoggerMessage(
        2037,
        LogLevel.Debug,
        "Successfully committed offset {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_OffsetCommitted")]
    private static partial void OffsetCommitted(ILogger logger, string topic, int partition, long offset, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs committing the offset.
    /// </summary>
    [LoggerMessage(
        2038,
        LogLevel.Error,
        "Error occurred committing offset {Topic}[{Partition}]@{Offset}: '{ErrorReason}' ({ErrorCode}) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_OffsetCommitError")]
    private static partial void OffsetCommitError(
        ILogger logger,
        string topic,
        int partition,
        long offset,
        string errorReason,
        int errorCode,
        string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non-fatal error is reported
    ///     by the <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Fatal errors are reported with a different event id.
    /// </remarks>
    [LoggerMessage(
        2039,
        LogLevel.Warning,
        "Error in Kafka consumer: '{ErrorReason}' ({ErrorCode}) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerError")]
    private static partial void ConfluentConsumerError(ILogger logger, string errorReason, int errorCode, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
    ///     the <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Non-fatal errors are reported with a different event id.
    /// </remarks>
    [LoggerMessage(
        2040,
        LogLevel.Error,
        "Fatal error in Kafka consumer: '{ErrorReason}' ({ErrorCode}) | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerFatalError")]
    private static partial void ConfluentConsumerFatalError(ILogger logger, string errorReason, int errorCode, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer statistics are
    ///     received.
    /// </summary>
    [LoggerMessage(
        2041,
        LogLevel.Debug,
        "Kafka consumer statistics received: {Statistics} | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConsumerStatisticsReceived")]
    private static partial void ConsumerStatisticsReceived(ILogger logger, string statistics, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the producer statistics are
    ///     received.
    /// </summary>
    [LoggerMessage(
        2042,
        LogLevel.Debug,
        "Kafka producer statistics received: {Statistics} | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ProducerStatisticsReceived")]
    private static partial void ProducerStatisticsReceived(ILogger logger, string statistics, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the statistics JSON cannot be
    ///     deserialized.
    /// </summary>
    [LoggerMessage(
        2043,
        LogLevel.Error,
        "Statistics JSON couldn't be deserialized",
        EventName = "Silverback.Integration.Kafka_StatisticsDeserializationError")]
    private static partial void StatisticsDeserializationError(ILogger logger, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a poll timeout is notified.
    ///     The consumer will automatically recover from this situation (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    [LoggerMessage(
        2060,
        LogLevel.Warning,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}'; the consumer will try to recover | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PollTimeoutAutoRecovery")]
    private static partial void PollTimeoutAutoRecovery(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a poll timeout is notified.
    ///     The consumer will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
    /// </summary>
    [LoggerMessage(
        2061,
        LogLevel.Error,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}'; auto recovery disabled for consumer | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_PollTimeoutNoAutoRecovery")]
    private static partial void PollTimeoutNoAutoRecovery(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the transactional producer has been initialized.
    /// </summary>
    [LoggerMessage(
        2070,
        LogLevel.Trace,
        "Transactions initialized | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}",
        EventName = "Silverback.Integration.Kafka_TransactionsInitialized")]
    private static partial void TransactionsInitialized(ILogger logger, string producerName, string? transactionalId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new transaction is started.
    /// </summary>
    [LoggerMessage(
        2071,
        LogLevel.Trace,
        "Transaction started | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}",
        EventName = "Silverback.Integration.Kafka_TransactionStarted")]
    private static partial void TransactionStarted(ILogger logger, string producerName, string? transactionalId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a transaction is committed.
    /// </summary>
    [LoggerMessage(
        2072,
        LogLevel.Information,
        "Transaction committed | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}",
        EventName = "Silverback.Integration.Kafka_TransactionCommitted")]
    private static partial void TransactionCommitted(ILogger logger, string producerName, string? transactionalId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a transaction is aborted.
    /// </summary>
    [LoggerMessage(
        2073,
        LogLevel.Information,
        "Transaction aborted | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}",
        EventName = "Silverback.Integration.Kafka_TransactionAborted")]
    private static partial void TransactionAborted(ILogger logger, string producerName, string? transactionalId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an offset is sent to the transaction.
    /// </summary>
    [LoggerMessage(
        2074,
        LogLevel.Debug,
        "Offset {Topic}[{Partition}]@{Offset} sent to transaction | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}",
        EventName = "Silverback.Integration.Kafka_OffsetSentToTransaction")]
    private static partial void OffsetSentToTransaction(
        ILogger logger,
        string topic,
        int partition,
        long offset,
        string producerName,
        string? transactionalId);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non-fatal error is reported
    ///     by the <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Fatal errors are reported with a different event id.
    /// </remarks>
    [LoggerMessage(
        2199,
        LogLevel.Warning,
        "Error in Kafka producer: '{ErrorReason}' ({ErrorCode}) | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerError")]
    private static partial void ConfluentProducerError(ILogger logger, string errorReason, int errorCode, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
    ///     the <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Non-fatal errors are reported with a different event id.
    /// </remarks>
    [LoggerMessage(
        2200,
        LogLevel.Error,
        "Fatal error in Kafka producer: '{ErrorReason}' ({ErrorCode}) | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerFatalError")]
    private static partial void ConfluentProducerFatalError(ILogger logger, string errorReason, int errorCode, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2201,
        LogLevel.Critical,
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerLogCritical")]
    private static partial void ConfluentProducerLogCritical(ILogger logger, string sysLogLevel, string logMessage, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2202,
        LogLevel.Error,
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerLogError")]
    private static partial void ConfluentProducerLogError(ILogger logger, string sysLogLevel, string logMessage, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2203,
        LogLevel.Warning,
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerLogWarning")]
    private static partial void ConfluentProducerLogWarning(ILogger logger, string sysLogLevel, string logMessage, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2204,
        LogLevel.Information,
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerLogInformation")]
    private static partial void ConfluentProducerLogInformation(ILogger logger, string sysLogLevel, string logMessage, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IProducer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2205,
        LogLevel.Debug,
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentProducerLogDebug")]
    private static partial void ConfluentProducerLogDebug(ILogger logger, string sysLogLevel, string logMessage, string producerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2211,
        LogLevel.Critical,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerLogCritical")]
    private static partial void ConfluentConsumerLogCritical(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2212,
        LogLevel.Error,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerLogError")]
    private static partial void ConfluentConsumerLogError(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2213,
        LogLevel.Warning,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerLogWarning")]
    private static partial void ConfluentConsumerLogWarning(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2214,
        LogLevel.Information,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerLogInformation")]
    private static partial void ConfluentConsumerLogInformation(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IConsumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2215,
        LogLevel.Debug,
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}",
        EventName = "Silverback.Integration.Kafka_ConfluentConsumerLogDebug")]
    private static partial void ConfluentConsumerLogDebug(ILogger logger, string sysLogLevel, string logMessage, string consumerName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non-fatal error is reported
    ///     by the <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     Fatal errors are reported with a different event id.
    /// </remarks>
    [LoggerMessage(
        2301,
        LogLevel.Warning,
        "Error in Kafka admin client: '{ErrorReason}' ({ErrorCode})",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientError")]
    private static partial void ConfluentAdminClientError(ILogger logger, string errorReason, int errorCode);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
    ///     the <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     Non-fatal errors are reported with a different event id.
    /// </remarks>
    [LoggerMessage(
        2302,
        LogLevel.Error,
        "Fatal error in Kafka admin client: '{ErrorReason}' ({ErrorCode})",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientFatalError")]
    private static partial void ConfluentAdminClientFatalError(ILogger logger, string errorReason, int errorCode);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2311,
        LogLevel.Critical,
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientLogCritical")]
    private static partial void ConfluentAdminClientLogCritical(ILogger logger, string sysLogLevel, string logMessage);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2312,
        LogLevel.Error,
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientLogError")]
    private static partial void ConfluentAdminClientLogError(ILogger logger, string sysLogLevel, string logMessage);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2313,
        LogLevel.Warning,
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientLogWarning")]
    private static partial void ConfluentAdminClientLogWarning(ILogger logger, string sysLogLevel, string logMessage);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2314,
        LogLevel.Information,
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientLogInformation")]
    private static partial void ConfluentAdminClientLogInformation(ILogger logger, string sysLogLevel, string logMessage);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.IAdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    [LoggerMessage(
        2315,
        LogLevel.Debug,
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'",
        EventName = "Silverback.Integration.Kafka_ConfluentAdminClientLogDebug")]
    private static partial void ConfluentAdminClientLogDebug(ILogger logger, string sysLogLevel, string logMessage);
}
