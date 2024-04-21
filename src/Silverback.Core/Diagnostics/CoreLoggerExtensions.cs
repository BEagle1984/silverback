// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Background;

namespace Silverback.Diagnostics;

internal static class CoreLoggerExtensions
{
    private static readonly Action<ILogger, string?, string, Exception?> SubscriberResultDiscardedAction =
        SilverbackLoggerMessage.Define<string?, string>(CoreLogEvents.SubscriberResultDiscarded);

    private static readonly Action<ILogger, string, Exception?> BackgroundServiceStarting =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.BackgroundServiceStarting);

    private static readonly Action<ILogger, string, Exception?> BackgroundServiceException =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.BackgroundServiceException);

    private static readonly Action<ILogger, string, Exception?> RecurringBackgroundServiceStopped =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.RecurringBackgroundServiceStopped);

    private static readonly Action<ILogger, string, double, Exception?> RecurringBackgroundServiceSleeping =
        SilverbackLoggerMessage.Define<string, double>(CoreLogEvents.RecurringBackgroundServiceSleeping);

    private static readonly Action<ILogger, string, Exception?> LockAcquired =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.LockAcquired);

    private static readonly Action<ILogger, string, Exception?> LockReleased =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.LockReleased);

    private static readonly Action<ILogger, string, Exception?> LockLost =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.LockLost);

    private static readonly Action<ILogger, string, Exception?> AcquireLockFailed =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.AcquireLockFailed);

    private static readonly Action<ILogger, string, Exception?> AcquireLockConcurrencyException =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.AcquireLockConcurrencyException);

    private static readonly Action<ILogger, string, Exception?> ReleaseLockFailed =
        SilverbackLoggerMessage.Define<string>(CoreLogEvents.ReleaseLockFailed);

    public static void LogSubscriberResultDiscarded(
        this ISilverbackLogger logger,
        string? resultTypeName,
        string expectedReturnTypeName) =>
        SubscriberResultDiscardedAction(logger.InnerLogger, resultTypeName, expectedReturnTypeName, null);

    public static void LogBackgroundServiceStarting(
        this ISilverbackLogger logger,
        DistributedBackgroundService service) =>
        BackgroundServiceStarting(logger.InnerLogger, service.GetType().FullName!, null);

    public static void LogBackgroundServiceException(
        this ISilverbackLogger logger,
        DistributedBackgroundService service,
        Exception exception) =>
        BackgroundServiceException(logger.InnerLogger, service.GetType().FullName!, exception);

    public static void LogRecurringBackgroundServiceStopped(
        this ISilverbackLogger logger,
        DistributedBackgroundService service) =>
        RecurringBackgroundServiceStopped(logger.InnerLogger, service.GetType().FullName!, null);

    public static void LogRecurringBackgroundServiceSleeping(
        this ISilverbackLogger logger,
        DistributedBackgroundService service,
        TimeSpan delay) =>
        RecurringBackgroundServiceSleeping(logger.InnerLogger, service.GetType().FullName!, delay.TotalMilliseconds, null);

    public static void LogLockAcquired(this ISilverbackLogger logger, string lockName) =>
        LockAcquired(logger.InnerLogger, lockName, null);

    public static void LogLockReleased(this ISilverbackLogger logger, string lockName) =>
        LockReleased(logger.InnerLogger, lockName, null);

    public static void LogLockLost(this ISilverbackLogger logger, string lockName, Exception? exception = null) =>
        LockLost(logger.InnerLogger, lockName, exception);

    public static void LogAcquireLockFailed(this ISilverbackLogger logger, string lockName, Exception exception) =>
        AcquireLockFailed(logger.InnerLogger, lockName, exception);

    public static void LogAcquireLockConcurrencyException(this ISilverbackLogger logger, string lockName, Exception exception) =>
        AcquireLockConcurrencyException(logger.InnerLogger, lockName, exception);

    public static void LogReleaseLockFailed(this ISilverbackLogger logger, string lockName, Exception exception) =>
        ReleaseLockFailed(logger.InnerLogger, lockName, exception);
}
