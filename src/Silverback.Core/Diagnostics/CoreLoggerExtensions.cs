// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Background;

namespace Silverback.Diagnostics;

internal static class CoreLoggerExtensions
{
    private static readonly Action<ILogger, string?, string, Exception?>
        SubscriberResultDiscardedAction = SilverbackLoggerMessage.Define<string?, string>(CoreLogEvents.SubscriberResultDiscarded);

    private static readonly Action<ILogger, string, Exception?>
        BackgroundServiceStarting = SilverbackLoggerMessage.Define<string>(CoreLogEvents.BackgroundServiceStarting);

    private static readonly Action<ILogger, string, Exception?>
        BackgroundServiceLockAcquired = SilverbackLoggerMessage.Define<string>(CoreLogEvents.BackgroundServiceLockAcquired);

    private static readonly Action<ILogger, string, Exception?>
        BackgroundServiceException = SilverbackLoggerMessage.Define<string>(CoreLogEvents.BackgroundServiceException);

    private static readonly Action<ILogger, string, Exception?>
        RecurringBackgroundServiceStopped = SilverbackLoggerMessage.Define<string>(CoreLogEvents.RecurringBackgroundServiceStopped);

    private static readonly Action<ILogger, string, double, Exception?>
        RecurringBackgroundServiceSleeping = SilverbackLoggerMessage.Define<string, double>(CoreLogEvents.RecurringBackgroundServiceSleeping);

    public static void LogSubscriberResultDiscarded(
        this ISilverbackLogger logger,
        string? resultTypeName,
        string expectedReturnTypeName) =>
        SubscriberResultDiscardedAction(logger.InnerLogger, resultTypeName, expectedReturnTypeName, null);

    public static void LogBackgroundServiceStarting(
        this ISilverbackLogger logger,
        DistributedBackgroundService service) =>
        BackgroundServiceStarting(logger.InnerLogger, service.GetType().FullName!, null);

    public static void LogBackgroundServiceLockAcquired(
        this ISilverbackLogger logger,
        DistributedBackgroundService service) =>
        BackgroundServiceLockAcquired(logger.InnerLogger, service.GetType().FullName!, null);

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
}
