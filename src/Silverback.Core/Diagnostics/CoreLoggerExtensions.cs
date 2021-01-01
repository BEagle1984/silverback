// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Background;

namespace Silverback.Diagnostics
{
    internal static class CoreLoggerExtensions
    {
        private static readonly Action<ILogger, string?, string, Exception?>
            SubscriberResultDiscardedAction = SilverbackLoggerMessage.Define<string?, string>(
                CoreLogEvents.SubscriberResultDiscarded);

        private static readonly Action<ILogger, string, string, Exception?>
            AcquiringLock = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.AcquiringDistributedLock);

        private static readonly Action<ILogger, string, string, Exception?>
            LockAcquired = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.DistributedLockAcquired);

        private static readonly Action<ILogger, string, string, Exception?>
            FailedToAcquireLock = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.FailedToAcquireDistributedLock);

        private static readonly Action<ILogger, string, string, Exception?>
            LockReleased = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.DistributedLockReleased);

        private static readonly Action<ILogger, string, string, Exception?>
            FailedToReleaseLock = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.FailedToReleaseDistributedLock);

        private static readonly Action<ILogger, string, string, Exception?>
            FailedToCheckLock = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.FailedToCheckDistributedLock);

        private static readonly Action<ILogger, string, string, Exception?>
            FailedToSendLockHeartbeat = SilverbackLoggerMessage.Define<string, string>(
                CoreLogEvents.FailedToSendDistributedLockHeartbeat);

        private static readonly Action<ILogger, string, Exception?>
            BackgroundServiceStarting = SilverbackLoggerMessage.Define<string>(
                CoreLogEvents.BackgroundServiceStarting);

        private static readonly Action<ILogger, string, Exception?>
            BackgroundServiceLockAcquired = SilverbackLoggerMessage.Define<string>(
                CoreLogEvents.BackgroundServiceLockAcquired);

        private static readonly Action<ILogger, string, Exception?>
            BackgroundServiceException = SilverbackLoggerMessage.Define<string>(
                CoreLogEvents.BackgroundServiceException);

        private static readonly Action<ILogger, string, Exception?>
            RecurringBackgroundServiceStopped = SilverbackLoggerMessage.Define<string>(
                CoreLogEvents.RecurringBackgroundServiceStopped);

        private static readonly Action<ILogger, string, double, Exception?>
            RecurringBackgroundServiceSleeping = SilverbackLoggerMessage.Define<string, double>(
                CoreLogEvents.RecurringBackgroundServiceSleeping);

        private static readonly Action<ILogger, string, Exception?>
            RecurringBackgroundServiceException = SilverbackLoggerMessage.Define<string>(
                CoreLogEvents.RecurringBackgroundServiceException);

        public static void LogSubscriberResultDiscarded(
            this ISilverbackLogger logger,
            string? resultTypeName,
            string expectedReturnTypeName) =>
            SubscriberResultDiscardedAction(logger.InnerLogger, resultTypeName, expectedReturnTypeName, null);

        public static void LogAcquiringLock(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings) =>
            AcquiringLock(logger.InnerLogger, lockSettings.ResourceName, lockSettings.UniqueId, null);

        public static void LogLockAcquired(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings) =>
            LockAcquired(logger.InnerLogger, lockSettings.ResourceName, lockSettings.UniqueId, null);

        public static void LogFailedToAcquireLock(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings,
            Exception exception) =>
            FailedToAcquireLock(
                logger.InnerLogger,
                lockSettings.ResourceName,
                lockSettings.UniqueId,
                exception);

        public static void LogLockReleased(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings) =>
            LockReleased(logger.InnerLogger, lockSettings.ResourceName, lockSettings.UniqueId, null);

        public static void LogFailedToReleaseLock(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings,
            Exception exception) =>
            FailedToReleaseLock(
                logger.InnerLogger,
                lockSettings.ResourceName,
                lockSettings.UniqueId,
                exception);

        public static void LogFailedToCheckLock(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings,
            Exception exception) =>
            FailedToCheckLock(
                logger.InnerLogger,
                lockSettings.ResourceName,
                lockSettings.UniqueId,
                exception);

        public static void LogFailedToSendLockHeartbeat(
            this ISilverbackLogger logger,
            DistributedLockSettings lockSettings,
            Exception exception) =>
            FailedToSendLockHeartbeat(
                logger.InnerLogger,
                lockSettings.ResourceName,
                lockSettings.UniqueId,
                exception);

        public static void LogBackgroundServiceStarting(
            this ISilverbackLogger logger,
            DistributedBackgroundService service) =>
            BackgroundServiceStarting(logger.InnerLogger, service.GetType().FullName, null);

        public static void LogBackgroundServiceLockAcquired(
            this ISilverbackLogger logger,
            DistributedBackgroundService service) =>
            BackgroundServiceLockAcquired(logger.InnerLogger, service.GetType().FullName, null);

        public static void LogBackgroundServiceException(
            this ISilverbackLogger logger,
            DistributedBackgroundService service,
            Exception exception) =>
            BackgroundServiceException(logger.InnerLogger, service.GetType().FullName, exception);

        public static void LogRecurringBackgroundServiceStopped(
            this ISilverbackLogger logger,
            DistributedBackgroundService service) =>
            RecurringBackgroundServiceStopped(logger.InnerLogger, service.GetType().FullName, null);

        public static void LogRecurringBackgroundServiceSleeping(
            this ISilverbackLogger logger,
            DistributedBackgroundService service,
            TimeSpan delay) =>
            RecurringBackgroundServiceSleeping(
                logger.InnerLogger,
                service.GetType().FullName,
                delay.TotalMilliseconds,
                null);

        public static void LogRecurringBackgroundServiceException(
            this ISilverbackLogger logger,
            DistributedBackgroundService service,
            Exception exception) =>
            RecurringBackgroundServiceException(logger.InnerLogger, service.GetType().FullName, exception);
    }
}
