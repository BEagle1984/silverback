// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Messaging.Publishing;

namespace Silverback.Diagnostics;

// The CoreLogEvents properties are generated from the [LoggerMessage] methods, including their documentation
internal static partial class CoreLoggerExtensions
{
    public static void LogSubscriberResultDiscarded(
        this ISilverbackLogger logger,
        string? resultTypeName,
        string expectedReturnTypeName) =>
        SubscriberResultDiscarded(logger.InnerLogger, resultTypeName, expectedReturnTypeName);

    public static void LogBackgroundServiceStarting(
        this ISilverbackLogger logger,
        DistributedBackgroundService service) =>
        BackgroundServiceStarting(logger.InnerLogger, service.GetType().FullName!);

    public static void LogBackgroundServiceException(
        this ISilverbackLogger logger,
        DistributedBackgroundService service,
        Exception exception) =>
        BackgroundServiceException(logger.InnerLogger, service.GetType().FullName!, exception);

    public static void LogRecurringBackgroundServiceStopped(
        this ISilverbackLogger logger,
        DistributedBackgroundService service) =>
        RecurringBackgroundServiceStopped(logger.InnerLogger, service.GetType().FullName!);

    public static void LogRecurringBackgroundServiceSleeping(
        this ISilverbackLogger logger,
        DistributedBackgroundService service,
        TimeSpan delay) =>
        RecurringBackgroundServiceSleeping(logger.InnerLogger, service.GetType().FullName!, delay.TotalMilliseconds);

    public static void LogLockAcquired(this ISilverbackLogger logger, string lockName) =>
        LockAcquired(logger.InnerLogger, lockName);

    public static void LogLockReleased(this ISilverbackLogger logger, string lockName) =>
        LockReleased(logger.InnerLogger, lockName);

    public static void LogLockLost(this ISilverbackLogger logger, string lockName, Exception? exception = null) =>
        LockLost(logger.InnerLogger, lockName, exception);

    public static void LogAcquireLockFailed(this ISilverbackLogger logger, string lockName, Exception exception) =>
        AcquireLockFailed(logger.InnerLogger, lockName, exception);

    public static void LogAcquireLockConcurrencyException(this ISilverbackLogger logger, string lockName, Exception exception) =>
        AcquireLockConcurrencyException(logger.InnerLogger, lockName, exception);

    public static void LogReleaseLockFailed(this ISilverbackLogger logger, string lockName, Exception exception) =>
        ReleaseLockFailed(logger.InnerLogger, lockName, exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the <see cref="IPublisher" />
    ///     discards the return value of a subscribed method because it doesn't match with the expected return type.
    /// </summary>
    [LoggerMessage(
        11,
        LogLevel.Debug,
        "Discarding result of type {Type} because doesn't match expected return type {ExpectedType}",
        EventName = "Silverback.Core_SubscriberResultDiscarded")]
    private static partial void SubscriberResultDiscarded(ILogger logger, string? type, string expectedType);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written the
    ///     <see cref="DistributedBackgroundService" /> is starting.
    /// </summary>
    [LoggerMessage(
        41,
        LogLevel.Information,
        "Starting background service {BackgroundService}",
        EventName = "Silverback.Core_BackgroundServiceStarting")]
    private static partial void BackgroundServiceStarting(ILogger logger, string backgroundService);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an error occurs executing the
    ///     <see cref="DistributedBackgroundService" />.
    /// </summary>
    [LoggerMessage(
        42,
        LogLevel.Error,
        "Background service {BackgroundService} execution failed",
        EventName = "Silverback.Core_BackgroundServiceException")]
    private static partial void BackgroundServiceException(ILogger logger, string backgroundService, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="RecurringDistributedBackgroundService" /> is stopped.
    /// </summary>
    [LoggerMessage(
        51,
        LogLevel.Information,
        "Background service {BackgroundService} stopped",
        EventName = "Silverback.Core_RecurringBackgroundServiceStopped")]
    private static partial void RecurringBackgroundServiceStopped(ILogger logger, string backgroundService);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when the
    ///     <see cref="RecurringDistributedBackgroundService" /> is sleeping in between the executions.
    /// </summary>
    [LoggerMessage(
        52,
        LogLevel.Debug,
        "Background service {BackgroundService} sleeping for {Delay} ms",
        EventName = "Silverback.Core_RecurringBackgroundServiceSleeping")]
    private static partial void RecurringBackgroundServiceSleeping(ILogger logger, string backgroundService, double delay);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a distributed lock is acquired by the current instance.
    /// </summary>
    [LoggerMessage(
        61,
        LogLevel.Information,
        "Lock {LockName} acquired",
        EventName = "Silverback.Core_LockAcquired")]
    private static partial void LockAcquired(ILogger logger, string lockName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a distributed lock is released.
    /// </summary>
    [LoggerMessage(
        62,
        LogLevel.Information,
        "Lock {LockName} released",
        EventName = "Silverback.Core_LockReleased")]
    private static partial void LockReleased(ILogger logger, string lockName);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a distributed lock is lost.
    /// </summary>
    [LoggerMessage(
        63,
        LogLevel.Error,
        "Lock {LockName} lost",
        EventName = "Silverback.Core_LockLost")]
    private static partial void LockLost(ILogger logger, string lockName, Exception? exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception occurs while trying to acquire a lock.
    /// </summary>
    [LoggerMessage(
        64,
        LogLevel.Error,
        "Failed to acquire lock {LockName}",
        EventName = "Silverback.Core_AcquireLockFailed")]
    private static partial void AcquireLockFailed(ILogger logger, string lockName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when a concurrency related exception occurs while trying to
    ///     acquire a lock. This is usually not an issue and just means that the lock was acquired by another instance in the meantime.
    /// </summary>
    [LoggerMessage(
        65,
        LogLevel.Information,
        "Failed to acquire lock {LockName}",
        EventName = "Silverback.Core_AcquireLockConcurrencyException")]
    private static partial void AcquireLockConcurrencyException(ILogger logger, string lockName, Exception exception);

    /// <summary>
    ///     Gets the <see cref="LogEvent" /> representing the log that is written when an exception occurs while trying to release a lock.
    /// </summary>
    [LoggerMessage(
        66,
        LogLevel.Error,
        "Failed to release lock {LockName}",
        EventName = "Silverback.Core_ReleaseLockFailed")]
    private static partial void ReleaseLockFailed(ILogger logger, string lockName, Exception exception);
}
