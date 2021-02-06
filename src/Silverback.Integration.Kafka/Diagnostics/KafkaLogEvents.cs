// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="LogEvent" /> constants of all events logged by the
    ///     Silverback.Integration.Kafka package.
    /// </summary>
    [SuppressMessage("", "SA1118", Justification = "Cleaner and clearer this way")]
    public static class KafkaLogEvents
    {
        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a message is consumed from a
        ///     Kafka topic.
        /// </summary>
        public static LogEvent ConsumingMessage { get; } = new(
            LogLevel.Debug,
            GetEventId(11, nameof(ConsumingMessage)),
            "Consuming message: {topic}[{partition}]@{offset}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the end of partition is
        ///     reached.
        /// </summary>
        public static LogEvent EndOfPartition { get; } = new(
            LogLevel.Information,
            GetEventId(12, nameof(EndOfPartition)),
            "Partition EOF reached: {topic}[{partition}]@{offset}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a
        ///     <see cref="KafkaExceptionAutoRecovery" /> is thrown inside the <c>Consume</c> method. The consumer
        ///     will automatically recover from these exceptions (<c>EnableAutoRecovery</c> is <c>true</c>).
        /// </summary>
        public static LogEvent KafkaExceptionAutoRecovery { get; } = new(
            LogLevel.Warning,
            GetEventId(13, nameof(KafkaExceptionAutoRecovery)),
            "An error occurred while trying to pull the next message. The consumer will try to recover.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a
        ///     <see cref="KafkaExceptionAutoRecovery" /> is thrown inside the <c>Consume</c> method. The consumer
        ///     will be stopped (<c>EnableAutoRecovery</c> is <c>false</c>).
        /// </summary>
        public static LogEvent KafkaExceptionNoAutoRecovery { get; } = new(
            LogLevel.Error,
            GetEventId(14, nameof(KafkaExceptionNoAutoRecovery)),
            "An error occurred while trying to pull the next message. The consumer will be stopped. " +
            "Enable auto recovery to allow Silverback to automatically try to reconnect " +
            "(EnableAutoRecovery=true in the consumer configuration).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
        ///     <see cref="KafkaConsumer" /> is unable to recover from the <see cref="KafkaExceptionAutoRecovery" />.
        /// </summary>
        public static LogEvent ErrorRecoveringFromKafkaException { get; } = new(
            LogLevel.Warning,
            GetEventId(15, nameof(ErrorRecoveringFromKafkaException)),
            "Failed to recover from consumer exception. Will retry in {retryDelay} milliseconds.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the <c>Consume</c> is aborted
        ///     (usually because the broker is being disconnected or the application is exiting).
        /// </summary>
        public static LogEvent ConsumingCanceled { get; } = new(
            LogLevel.Trace,
            GetEventId(16, nameof(ConsumingCanceled)),
            "Consuming canceled.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
        ///     <see cref="Confluent.Kafka.Producer{TKey,TValue}" /> is being instantiated.
        /// </summary>
        public static LogEvent CreatingConfluentProducer { get; } = new(
            LogLevel.Debug,
            GetEventId(21, nameof(CreatingConfluentProducer)),
            "Creating Confluent.Kafka.Producer...");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the message is sent to the
        ///     broker but no acknowledge is received. This is logged only if <c>ThrowIfNotAcknowledged</c> is
        ///     <c>false</c>.
        /// </summary>
        public static LogEvent ProduceNotAcknowledged { get; } = new(
            LogLevel.Warning,
            GetEventId(22, nameof(ProduceNotAcknowledged)),
            "The message was transmitted to broker, but no acknowledgement was received.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a new consumer group partition
        ///     assignment has been received by a consumer.
        /// </summary>
        /// <remarks>
        ///     An event will be logged for each assigned partition.
        /// </remarks>
        public static LogEvent PartitionAssigned { get; } = new(
            LogLevel.Information,
            GetEventId(31, nameof(PartitionAssigned)),
            "Assigned partition {topic}[{partition}].");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the offset of an assigned
        ///     partition is being reset.
        /// </summary>
        public static LogEvent PartitionOffsetReset { get; } = new(
            LogLevel.Debug,
            GetEventId(32, nameof(PartitionOffsetReset)),
            "{topic}[{partition}] offset will be reset to {offset}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a group partition assignment
        ///     is being revoked.
        /// </summary>
        /// <remarks>
        ///     An event will be logged for each revoked partition.
        /// </remarks>
        public static LogEvent PartitionRevoked { get; } = new(
            LogLevel.Information,
            GetEventId(33, nameof(PartitionRevoked)),
            "Revoked partition {topic}[{partition}] (offset was {offset}).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an offset is successfully
        ///     committed.
        /// </summary>
        public static LogEvent OffsetCommitted { get; } = new(
            LogLevel.Debug,
            GetEventId(34, nameof(OffsetCommitted)),
            "Successfully committed offset {topic}[{partition}]@{offset}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs while
        ///     committing the offset.
        /// </summary>
        public static LogEvent OffsetCommitError { get; } = new(
            LogLevel.Error,
            GetEventId(35, nameof(OffsetCommitError)),
            "Error occurred committing the offset {topic}[{partition}]@{offset}: '{errorReason}' ({errorCode}).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a fatal error is reported by
        ///     the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
        /// </summary>
        /// <remarks>
        ///     Non fatal errors are reported with a different event id.
        /// </remarks>
        public static LogEvent ConfluentConsumerFatalError { get; } = new(
            LogLevel.Error,
            GetEventId(36, nameof(ConfluentConsumerFatalError)),
            "Fatal error in Kafka consumer: '{errorReason}' ({errorCode}).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the custom error handler
        ///     throws an unhandled exception.
        /// </summary>
        public static LogEvent KafkaErrorHandlerError { get; } = new(
            LogLevel.Error,
            GetEventId(37, nameof(KafkaErrorHandlerError)),
            "Error in Kafka consumer error handler.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer statistics are
        ///     received.
        /// </summary>
        public static LogEvent ConsumerStatisticsReceived { get; } = new(
            LogLevel.Debug,
            GetEventId(38, nameof(ConsumerStatisticsReceived)),
            "Kafka consumer statistics received: {statistics}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the producer statistics are
        ///     received.
        /// </summary>
        public static LogEvent ProducerStatisticsReceived { get; } = new(
            LogLevel.Debug,
            GetEventId(39, nameof(ProducerStatisticsReceived)),
            "Kafka producer statistics received: {statistics}");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the statistics JSOn cannot be
        ///     deserialized.
        /// </summary>
        public static LogEvent StatisticsDeserializationError { get; } = new(
            LogLevel.Error,
            GetEventId(40, nameof(StatisticsDeserializationError)),
            "The received statistics JSON couldn't be deserialized.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when the partitions are manually
        ///     assigned.
        /// </summary>
        /// <remarks>
        ///     An event will be logged for each assigned partition.
        /// </remarks>
        public static LogEvent PartitionManuallyAssigned { get; } = new(
            LogLevel.Information,
            GetEventId(41, nameof(PartitionManuallyAssigned)),
            "Assigned partition {topic}[{partition}]@{offset}.");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when a non fatal error is reported
        ///     by the <see cref="Confluent.Kafka.Consumer{TKey,TValue}" />.
        /// </summary>
        /// <remarks>
        ///     Fatal errors are reported with a different event id.
        /// </remarks>
        public static LogEvent ConfluentConsumerError { get; } = new(
            LogLevel.Warning,
            GetEventId(42, nameof(ConfluentConsumerError)),
            "Error in Kafka consumer: '{errorReason}' ({errorCode}).");

        /// <summary>
        ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception is thrown
        ///     disconnecting the consumer.
        /// </summary>
        public static LogEvent ConsumerDisconnectError { get; } = new(
            LogLevel.Warning,
            GetEventId(50, nameof(ConsumerDisconnectError)),
            "Error disconnecting consumer.");

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
            "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'.");

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
            "{sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'.");

        private static EventId GetEventId(int id, string name) =>
            new(2000 + id, $"Silverback.Integration.Kafka_{name}");
    }
}
