// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Contains the <see cref="LogEvent" /> constants of all events logged by the
///     Silverback.Integration.Kafka package.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Clearer and cleaner")]
public static class KafkaLogEvents
{
    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is consumed from a
    ///     Kafka topic.
    /// </summary>
    public static LogEvent ConsumingMessage { get; } = new(
        LogLevel.Debug,
        GetEventId(11, nameof(ConsumingMessage)),
        "Consuming message {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the end of partition is
    ///     reached.
    /// </summary>
    public static LogEvent EndOfPartition { get; } = new(
        LogLevel.Information,
        GetEventId(12, nameof(EndOfPartition)),
        "Partition EOF reached: {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <see cref="KafkaException" /> is thrown inside the
    ///     <c>Consume</c> method. The consumer will automatically recover from these exceptions (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    public static LogEvent KafkaExceptionAutoRecovery { get; } = new(
        LogLevel.Warning,
        GetEventId(13, nameof(KafkaExceptionAutoRecovery)),
        "Error occurred trying to pull next message; the consumer will try to recover | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <see cref="KafkaException" /> is thrown inside the
    ///     <c>Consume</c> method. The consumer will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
    /// </summary>
    public static LogEvent KafkaExceptionNoAutoRecovery { get; } = new(
        LogLevel.Error,
        GetEventId(14, nameof(KafkaExceptionNoAutoRecovery)),
        "Error occurred trying to pull next message; the consumer will be stopped (auto recovery disabled for consumer)" +
        " | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the <c>Consume</c> is aborted
    ///     (usually because the broker is being disconnected or the application is exiting).
    /// </summary>
    public static LogEvent ConsumingCanceled { get; } = new(
        LogLevel.Trace,
        GetEventId(16, nameof(ConsumingCanceled)),
        "Consuming canceled | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is sent to the
    ///     broker but no acknowledge is received. This is logged only if <c>ThrowIfNotAcknowledged</c> is
    ///     <c>false</c>.
    /// </summary>
    public static LogEvent ProduceNotAcknowledged { get; } = new(
        LogLevel.Warning,
        GetEventId(22, nameof(ProduceNotAcknowledged)),
        "Message produced to {Topic}[{Partition}] but not acknowledged | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a static partition assignment is set for the consumer.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each assigned partition.
    /// </remarks>
    public static LogEvent PartitionStaticallyAssigned { get; } = new(
        LogLevel.Information,
        GetEventId(31, nameof(PartitionStaticallyAssigned)),
        "Assigned partition {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer group partition
    ///     assignment has been received by a consumer.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each assigned partition.
    /// </remarks>
    public static LogEvent PartitionAssigned { get; } = new(
        LogLevel.Information,
        GetEventId(32, nameof(PartitionAssigned)),
        "Assigned partition {Topic}[{Partition}] | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the offset of an assigned
    ///     partition is being reset.
    /// </summary>
    public static LogEvent PartitionOffsetReset { get; } = new(
        LogLevel.Debug,
        GetEventId(33, nameof(PartitionOffsetReset)),
        "{Topic}[{Partition}] offset will be reset to {Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a group partition assignment
    ///     is being revoked.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each revoked partition.
    /// </remarks>
    public static LogEvent PartitionRevoked { get; } = new(
        LogLevel.Information,
        GetEventId(34, nameof(PartitionRevoked)),
        "Revoked partition {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a partition is paused.
    /// </summary>
    public static LogEvent PartitionPaused { get; } = new(
        LogLevel.Debug,
        GetEventId(35, nameof(PartitionPaused)),
        "Partition {Topic}[{Partition}] paused at offset {Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a partition is resumed.
    /// </summary>
    public static LogEvent PartitionResumed { get; } = new(
        LogLevel.Debug,
        GetEventId(36, nameof(PartitionResumed)),
        "Partition {Topic}[{Partition}] resumed | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an offset is successfully committed.
    /// </summary>
    public static LogEvent OffsetCommitted { get; } = new(
        LogLevel.Debug,
        GetEventId(37, nameof(OffsetCommitted)),
        "Successfully committed offset {Topic}[{Partition}]@{Offset} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs committing the offset.
    /// </summary>
    public static LogEvent OffsetCommitError { get; } = new(
        LogLevel.Error,
        GetEventId(38, nameof(OffsetCommitError)),
        "Error occurred committing offset {Topic}[{Partition}]@{Offset}: '{ErrorReason}' ({ErrorCode}) | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non-fatal error is reported
    ///     by the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Fatal errors are reported with a different event id.
    /// </remarks>
    public static LogEvent ConfluentConsumerError { get; } = new(
        LogLevel.Warning,
        GetEventId(39, nameof(ConfluentConsumerError)),
        "Error in Kafka consumer: '{ErrorReason}' ({ErrorCode}) | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
    ///     the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Non-fatal errors are reported with a different event id.
    /// </remarks>
    public static LogEvent ConfluentConsumerFatalError { get; } = new(
        LogLevel.Error,
        GetEventId(40, nameof(ConfluentConsumerFatalError)),
        "Fatal error in Kafka consumer: '{ErrorReason}' ({ErrorCode}) | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer statistics are
    ///     received.
    /// </summary>
    public static LogEvent ConsumerStatisticsReceived { get; } = new(
        LogLevel.Debug,
        GetEventId(41, nameof(ConsumerStatisticsReceived)),
        "Kafka consumer statistics received: {Statistics} | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the producer statistics are
    ///     received.
    /// </summary>
    public static LogEvent ProducerStatisticsReceived { get; } = new(
        LogLevel.Debug,
        GetEventId(42, nameof(ProducerStatisticsReceived)),
        "Kafka producer statistics received: {Statistics} | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the statistics JSON cannot be
    ///     deserialized.
    /// </summary>
    public static LogEvent StatisticsDeserializationError { get; } = new(
        LogLevel.Error,
        GetEventId(43, nameof(StatisticsDeserializationError)),
        "Statistics JSON couldn't be deserialized");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a poll timeout is notified.
    ///     The consumer will automatically recover from this situation (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    public static LogEvent PollTimeoutAutoRecovery { get; } = new(
        LogLevel.Warning,
        GetEventId(60, nameof(PollTimeoutAutoRecovery)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}'; the consumer will try to recover | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a poll timeout is notified.
    ///     The consumer will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
    /// </summary>
    public static LogEvent PollTimeoutNoAutoRecovery { get; } = new(
        LogLevel.Error,
        GetEventId(61, nameof(PollTimeoutNoAutoRecovery)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}'; auto recovery disabled for consumer | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the transactional producer has been initialized.
    /// </summary>
    public static LogEvent TransactionsInitialized { get; } = new(
        LogLevel.Trace,
        GetEventId(70, nameof(TransactionsInitialized)),
        "Transactions initialized | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new transaction is started.
    /// </summary>
    public static LogEvent TransactionStarted { get; } = new(
        LogLevel.Trace,
        GetEventId(71, nameof(TransactionStarted)),
        "Transaction started | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a transaction is committed.
    /// </summary>
    public static LogEvent TransactionCommitted { get; } = new(
        LogLevel.Information,
        GetEventId(72, nameof(TransactionCommitted)),
        "Transaction committed | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a transaction is aborted.
    /// </summary>
    public static LogEvent TransactionAborted { get; } = new(
        LogLevel.Information,
        GetEventId(73, nameof(TransactionAborted)),
        "Transaction aborted | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an offset is sent to the transaction.
    /// </summary>
    public static LogEvent OffsetSentToTransaction { get; } = new(
        LogLevel.Debug,
        GetEventId(74, nameof(OffsetSentToTransaction)),
        "Offset {Topic}[{Partition}]@{Offset} sent to transaction | ProducerName: {ProducerName}, TransactionalId: {TransactionalId}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Producer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentProducerLogCritical { get; } = new(
        LogLevel.Critical,
        GetEventId(201, nameof(ConfluentProducerLogCritical)),
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Producer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentProducerLogError { get; } = new(
        LogLevel.Error,
        GetEventId(202, nameof(ConfluentProducerLogError)),
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Producer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentProducerLogWarning { get; } = new(
        LogLevel.Warning,
        GetEventId(203, nameof(ConfluentProducerLogWarning)),
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Producer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentProducerLogInformation { get; } = new(
        LogLevel.Information,
        GetEventId(204, nameof(ConfluentProducerLogInformation)),
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Producer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentProducerLogDebug { get; } = new(
        LogLevel.Debug,
        GetEventId(205, nameof(ConfluentProducerLogDebug)),
        "{SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' | ProducerName: {ProducerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentConsumerLogCritical { get; } = new(
        LogLevel.Critical,
        GetEventId(211, nameof(ConfluentConsumerLogCritical)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentConsumerLogError { get; } = new(
        LogLevel.Error,
        GetEventId(212, nameof(ConfluentConsumerLogError)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentConsumerLogWarning { get; } = new(
        LogLevel.Warning,
        GetEventId(213, nameof(ConfluentConsumerLogWarning)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentConsumerLogInformation { get; } = new(
        LogLevel.Information,
        GetEventId(214, nameof(ConfluentConsumerLogInformation)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentConsumerLogDebug { get; } = new(
        LogLevel.Debug,
        GetEventId(215, nameof(ConfluentConsumerLogDebug)),
        "{SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non-fatal error is reported
    ///     by the <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     Fatal errors are reported with a different event id.
    /// </remarks>
    public static LogEvent ConfluentAdminClientError { get; } = new(
        LogLevel.Warning,
        GetEventId(301, nameof(ConfluentAdminClientError)),
        "Error in Kafka admin client: '{ErrorReason}' ({ErrorCode})");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
    ///     the <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     Non-fatal errors are reported with a different event id.
    /// </remarks>
    public static LogEvent ConfluentAdminClientFatalError { get; } = new(
        LogLevel.Error,
        GetEventId(302, nameof(ConfluentAdminClientFatalError)),
        "Fatal error in Kafka admin client: '{ErrorReason}' ({ErrorCode})");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentAdminClientLogCritical { get; } = new(
        LogLevel.Critical,
        GetEventId(311, nameof(ConfluentAdminClientLogCritical)),
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentAdminClientLogError { get; } = new(
        LogLevel.Error,
        GetEventId(312, nameof(ConfluentAdminClientLogError)),
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentAdminClientLogWarning { get; } = new(
        LogLevel.Warning,
        GetEventId(313, nameof(ConfluentAdminClientLogWarning)),
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentAdminClientLogInformation { get; } = new(
        LogLevel.Information,
        GetEventId(314, nameof(ConfluentAdminClientLogInformation)),
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a log event is received from
    ///     the underlying <see cref="Confluent.Kafka.AdminClient" />.
    /// </summary>
    /// <remarks>
    ///     A different event id is used per each log level.
    /// </remarks>
    public static LogEvent ConfluentAdminClientLogDebug { get; } = new(
        LogLevel.Debug,
        GetEventId(315, nameof(ConfluentAdminClientLogDebug)),
        "{SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}'");

    private static EventId GetEventId(int id, string name) =>
        new(2000 + id, $"Silverback.Integration.Kafka_{name}");
}
