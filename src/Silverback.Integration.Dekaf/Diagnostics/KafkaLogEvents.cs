// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Contains the <see cref="LogEvent" /> constants of all events logged by the
///     Silverback.Integration.Dekaf package.
/// </summary>
/// <remarks>
///     The event ids and names are the same as in Silverback.Integration.Kafka, so that the existing log filters and
///     queries keep working after switching the package.
/// </remarks>
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
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <c>Dekaf.Errors.KafkaException</c> is thrown inside the
    ///     <c>Consume</c> method. The consumer will automatically recover from these exceptions (<c>EnableAutoRecovery</c> is <c>true</c>).
    /// </summary>
    public static LogEvent KafkaExceptionAutoRecovery { get; } = new(
        LogLevel.Warning,
        GetEventId(13, nameof(KafkaExceptionAutoRecovery)),
        "Error occurred trying to pull next message; the consumer will try to recover | ConsumerName: {ConsumerName}");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a <c>Dekaf.Errors.KafkaException</c> is thrown inside the
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
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the consumer did not consume any message within the
    ///     stall detection threshold.
    /// </summary>
    public static LogEvent StaleConsumer { get; } = new(
        LogLevel.Information,
        GetEventId(17, nameof(StaleConsumer)),
        "No message consumed within the stall detection threshold ({StallDetectionThreshold}); the consumer will restart" +
        " | ConsumerName: {ConsumerName}");

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

    private static EventId GetEventId(int id, string name) =>
        new(2000 + id, $"Silverback.Integration.Kafka_{name}");
}
