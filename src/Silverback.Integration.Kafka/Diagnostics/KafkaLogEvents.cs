// Copyright (c) 2023 Sergio Aquilini
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
        "Consuming message {topic}[{partition}]@{offset}. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the end of partition is
    ///     reached.
    /// </summary>
    public static LogEvent EndOfPartition { get; } = new(
        LogLevel.Information,
        GetEventId(12, nameof(EndOfPartition)),
        "Partition EOF reached: {topic}[{partition}]@{offset}. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <see cref="KafkaException" /> is thrown inside the
    ///     <c>Consume</c> method. The consumer will automatically recover from these exceptions (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    public static LogEvent KafkaExceptionAutoRecovery { get; } = new(
        LogLevel.Warning,
        GetEventId(13, nameof(KafkaExceptionAutoRecovery)),
        "Error occurred trying to pull the next message. The consumer will try to recover. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <see cref="KafkaException" /> is thrown inside the
    ///     <c>Consume</c> method. The consumer will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
    /// </summary>
    public static LogEvent KafkaExceptionNoAutoRecovery { get; } = new(
        LogLevel.Error,
        GetEventId(14, nameof(KafkaExceptionNoAutoRecovery)),
        "Error occurred trying to pull the next message. The consumer will be stopped. " +
        "Enable auto recovery to allow Silverback to automatically try to recover " +
        "(EnableAutoRecovery=true in the consumer configuration)." +
        " | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the <c>Consume</c> is aborted
    ///     (usually because the broker is being disconnected or the application is exiting).
    /// </summary>
    public static LogEvent ConsumingCanceled { get; } = new(
        LogLevel.Trace,
        GetEventId(16, nameof(ConsumingCanceled)),
        "Consuming canceled. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is sent to the
    ///     broker but no acknowledge is received. This is logged only if <c>ThrowIfNotAcknowledged</c> is
    ///     <c>false</c>.
    /// </summary>
    public static LogEvent ProduceNotAcknowledged { get; } = new(
        LogLevel.Warning,
        GetEventId(22, nameof(ProduceNotAcknowledged)),
        "The message was produced to {topic}[{partition}], but no acknowledgement was received. | producerName: {producerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a static partition assignment is set for the consumer.
    /// </summary>
    /// <remarks>
    ///     An event will be logged for each assigned partition.
    /// </remarks>
    public static LogEvent PartitionStaticallyAssigned { get; } = new(
        LogLevel.Information,
        GetEventId(31, nameof(PartitionStaticallyAssigned)),
        "Assigned partition {topic}[{partition}]@{offset}. | consumerName: {consumerName}");

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
        "Assigned partition {topic}[{partition}]. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the offset of an assigned
    ///     partition is being reset.
    /// </summary>
    public static LogEvent PartitionOffsetReset { get; } = new(
        LogLevel.Debug,
        GetEventId(33, nameof(PartitionOffsetReset)),
        "{topic}[{partition}] offset will be reset to {offset}. | consumerName: {consumerName}");

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
        "Revoked partition {topic}[{partition}] (offset was {offset}). | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an offset is successfully committed.
    /// </summary>
    public static LogEvent OffsetCommitted { get; } = new(
        LogLevel.Debug,
        GetEventId(35, nameof(OffsetCommitted)),
        "Successfully committed offset {topic}[{partition}]@{offset}. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs committing the offset.
    /// </summary>
    public static LogEvent OffsetCommitError { get; } = new(
        LogLevel.Error,
        GetEventId(36, nameof(OffsetCommitError)),
        "Error occurred committing the offset {topic}[{partition}]@{offset}: '{errorReason}' ({errorCode}). | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non fatal error is reported
    ///     by the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Fatal errors are reported with a different event id.
    /// </remarks>
    public static LogEvent ConfluentConsumerError { get; } = new(
        LogLevel.Warning,
        GetEventId(36, nameof(ConfluentConsumerError)),
        "Error in Kafka consumer: '{errorReason}' ({errorCode}). | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
    ///     the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
    /// </summary>
    /// <remarks>
    ///     Non fatal errors are reported with a different event id.
    /// </remarks>
    public static LogEvent ConfluentConsumerFatalError { get; } = new(
        LogLevel.Error,
        GetEventId(37, nameof(ConfluentConsumerFatalError)),
        "Fatal error in Kafka consumer: '{errorReason}' ({errorCode}). | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer statistics are
    ///     received.
    /// </summary>
    public static LogEvent ConsumerStatisticsReceived { get; } = new(
        LogLevel.Debug,
        GetEventId(38, nameof(ConsumerStatisticsReceived)),
        "Kafka consumer statistics received: {statistics} | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the producer statistics are
    ///     received.
    /// </summary>
    public static LogEvent ProducerStatisticsReceived { get; } = new(
        LogLevel.Debug,
        GetEventId(39, nameof(ProducerStatisticsReceived)),
        "Kafka producer statistics received: {statistics} | producerName: {producerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the statistics JSON cannot be
    ///     deserialized.
    /// </summary>
    public static LogEvent StatisticsDeserializationError { get; } = new(
        LogLevel.Error,
        GetEventId(40, nameof(StatisticsDeserializationError)),
        "The received statistics JSON couldn't be deserialized.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a poll timeout is notified.
    ///     The consumer will automatically recover from these situation (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    public static LogEvent PollTimeoutAutoRecovery { get; } = new(
        LogLevel.Warning,
        GetEventId(60, nameof(PollTimeoutAutoRecovery)),
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. -> The consumer will try to recover. | consumerName: {consumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a poll timeout is notified.
    ///     The consumer will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
    /// </summary>
    public static LogEvent PollTimeoutNoAutoRecovery { get; } = new(
        LogLevel.Error,
        GetEventId(61, nameof(PollTimeoutNoAutoRecovery)),
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. -> Enable auto recovery to " +
        "allow Silverback to automatically try to recover (EnableAutoRecovery=true in the consumer " +
        "configuration). | consumerName: {consumerName}");

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
        "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | producerName: {producerName}");

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
        "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | producerName: {producerName}");

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
        "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | producerName: {producerName}");

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
        "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | producerName: {producerName}");

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
        "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | producerName: {producerName}");

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
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | consumerName: {consumerName}");

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
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | consumerName: {consumerName}");

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
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | consumerName: {consumerName}");

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
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | consumerName: {consumerName}");

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
        "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | consumerName: {consumerName}");

    private static EventId GetEventId(int id, string name) =>
        new(2000 + id, $"Silverback.Integration.Kafka_{name}");
}
