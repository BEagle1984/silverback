// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent AcquiringDistributedLock { get; } = new(
        LogLevel.Information,
        GetEventId(21, nameof(AcquiringDistributedLock)),
        "Not used anymore.");

    /// <summary>
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent DistributedLockAcquired { get; } = new(
        LogLevel.Information,
        GetEventId(22, nameof(DistributedLockAcquired)),
        "Not used anymore.");

    /// <summary>
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent FailedToAcquireDistributedLock { get; } = new(
        LogLevel.Debug,
        GetEventId(23, nameof(FailedToAcquireDistributedLock)),
        "Not used anymore.");

    /// <summary>
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent DistributedLockReleased { get; } = new(
        LogLevel.Information,
        GetEventId(24, nameof(DistributedLockReleased)),
        "Not used anymore.");

    /// <summary>
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent FailedToReleaseDistributedLock { get; } = new(
        LogLevel.Warning,
        GetEventId(25, nameof(FailedToReleaseDistributedLock)),
        "Not used anymore.");

    /// <summary>
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent FailedToCheckDistributedLock { get; } = new(
        LogLevel.Error,
        GetEventId(26, nameof(FailedToCheckDistributedLock)),
        "Not used anymore.");

    /// <summary>
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent FailedToSendDistributedLockHeartbeat { get; } = new(
        LogLevel.Error,
        GetEventId(27, nameof(FailedToSendDistributedLockHeartbeat)),
        "Not used anymore.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written the
    ///     <see cref="DistributedBackgroundService" /> is starting.
    /// </summary>
    public static LogEvent BackgroundServiceStarting { get; } = new(
        LogLevel.Information,
        GetEventId(41, nameof(BackgroundServiceStarting)),
        "Starting background service {backgroundService}...");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the lock has been acquired and
    ///     the
    ///     <see cref="DistributedBackgroundService" /> is being executed.
    /// </summary>
    public static LogEvent BackgroundServiceLockAcquired { get; } = new(
        LogLevel.Information,
        GetEventId(42, nameof(BackgroundServiceLockAcquired)),
        "Lock acquired, executing background service {backgroundService}.");

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs executing the
    ///     <see cref="DistributedBackgroundService" />.
    /// </summary>
    public static LogEvent BackgroundServiceException { get; } = new(
        LogLevel.Error,
        GetEventId(43, nameof(BackgroundServiceException)),
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
    ///     Reserved, not used anymore.
    /// </summary>
    [SuppressMessage("", "SA1623", Justification = "Reserved id")]
    [Obsolete("Not used anymore.", true)]
    public static LogEvent RecurringBackgroundServiceException { get; } = new(
        LogLevel.Warning,
        GetEventId(53, nameof(RecurringBackgroundServiceException)),
        "Not used anymore.");

    private static EventId GetEventId(int id, string name) => new(id, $"Silverback.Core_{name}");
}
