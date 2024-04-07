// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Messaging.Publishing;

namespace Silverback.Diagnostics;

/// <summary>
///     Contains the <see cref="LogEvent" /> constants of all events logged by the Silverback.Core package.
/// </summary>
[SuppressMessage("ReSharper", "SA1118", Justification = "Cleaner and clearer this way")]
public static class CoreLogEvents
{
    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the <see cref="IPublisher" />
    ///     discards the return value of a subscribed method because it doesn't match with the expected return type.
    /// </summary>
    public static LogEvent SubscriberResultDiscarded { get; } = new(
        LogLevel.Debug,
        GetEventId(11, nameof(SubscriberResultDiscarded)),
        "Discarding result of type {type} because it doesn't match the expected return type " +
        "{expectedType}.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written the
    ///     <see cref="DistributedBackgroundService" /> is starting.
    /// </summary>
    public static LogEvent BackgroundServiceStarting { get; } = new(
        LogLevel.Information,
        GetEventId(41, nameof(BackgroundServiceStarting)),
        "Starting background service {backgroundService}...");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs executing the
    ///     <see cref="DistributedBackgroundService" />.
    /// </summary>
    public static LogEvent BackgroundServiceException { get; } = new(
        LogLevel.Error,
        GetEventId(42, nameof(BackgroundServiceException)),
        "Background service {backgroundService} execution failed.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="RecurringDistributedBackgroundService" /> is stopped.
    /// </summary>
    public static LogEvent RecurringBackgroundServiceStopped { get; } = new(
        LogLevel.Information,
        GetEventId(51, nameof(RecurringBackgroundServiceStopped)),
        "Background service {backgroundService} stopped.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="RecurringDistributedBackgroundService" /> is sleeping in between the executions.
    /// </summary>
    public static LogEvent RecurringBackgroundServiceSleeping { get; } = new(
        LogLevel.Debug,
        GetEventId(52, nameof(RecurringBackgroundServiceSleeping)),
        "Background service {backgroundService} sleeping for {delay} milliseconds.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a distributed lock is acquired by the current instance.
    /// </summary>
    public static LogEvent LockAcquired { get; } = new(
        LogLevel.Information,
        GetEventId(61, nameof(LockAcquired)),
        "Lock {lockName} acquired.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a distributed lock is released.
    /// </summary>
    public static LogEvent LockReleased { get; } = new(
        LogLevel.Information,
        GetEventId(62, nameof(LockAcquired)),
        "Lock {lockName} released.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a distributed lock is lost.
    /// </summary>
    public static LogEvent LockLost { get; } = new(
        LogLevel.Error,
        GetEventId(63, nameof(LockLost)),
        "Lock {lockName} lost.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception occurs while trying to acquire a lock.
    /// </summary>
    public static LogEvent AcquireLockFailed { get; } = new(
        LogLevel.Error,
        GetEventId(64, nameof(LockLost)),
        "Failed to acquire lock {lockName}.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a concurrency related exception occurs while trying to
    ///     acquire a lock. This is usually not an issue and just means that the lock was acquired by another instance in the meantime.
    /// </summary>
    public static LogEvent AcquireLockConcurrencyException { get; } = new(
        LogLevel.Information,
        GetEventId(65, nameof(LockLost)),
        "Failed to acquire lock {lockName}.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception occurs while trying to release a lock.
    /// </summary>
    public static LogEvent ReleaseLockFailed { get; } = new(
        LogLevel.Error,
        GetEventId(66, nameof(LockLost)),
        "Failed to release lock {lockName}.");

    private static EventId GetEventId(int id, string name) => new(id, $"Silverback.Core_{name}");
}
