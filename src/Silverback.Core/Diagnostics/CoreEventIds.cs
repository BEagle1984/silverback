// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Background;
using Silverback.Messaging.Publishing;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Contains the <see cref="EventId" /> constants of all events logged by the Silverback.Core package.
    /// </summary>
    public static class CoreEventIds
    {
        private const string Prefix = "Silverback.Core_";

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the <see cref="IPublisher" /> discards
        ///     the return value of a subscribed method because it doesn't match with the expected return type.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId SubscriberResultDiscarded { get; } =
            new EventId(11, Prefix + nameof(SubscriberResultDiscarded));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IDistributedLockManager" /> start trying to acquire a lock.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId AcquiringDistributedLock { get; } =
            new EventId(21, Prefix + nameof(AcquiringDistributedLock));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IDistributedLockManager" /> acquires a lock.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId DistributedLockAcquired { get; } =
            new EventId(22, Prefix + nameof(DistributedLockAcquired));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the
        ///     <see cref="IDistributedLockManager" /> acquires a lock.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId FailedToAcquireDistributedLock { get; } =
            new EventId(23, Prefix + nameof(FailedToAcquireDistributedLock));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="IDistributedLockManager" /> releases a lock.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId DistributedLockReleased { get; } =
            new EventId(24, Prefix + nameof(DistributedLockReleased));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the
        ///     <see cref="IDistributedLockManager" /> releases a lock.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId FailedToReleaseDistributedLock { get; } =
            new EventId(25, Prefix + nameof(FailedToReleaseDistributedLock));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the
        ///     <see cref="IDistributedLockManager" /> checks whether a lock is still valid.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId FailedToCheckDistributedLock { get; } =
            new EventId(26, Prefix + nameof(FailedToCheckDistributedLock));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs while the
        ///     <see cref="IDistributedLockManager" /> tries to send the heartbeat to keep the lock alive.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId FailedToSendDistributedLockHeartbeat { get; } =
            new EventId(27, Prefix + nameof(FailedToSendDistributedLockHeartbeat));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written the
        ///     <see cref="DistributedBackgroundService" /> is starting.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId BackgroundServiceStarting { get; } =
            new EventId(41, Prefix + nameof(BackgroundServiceStarting));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the lock has been acquired and the
        ///     <see cref="DistributedBackgroundService" /> is being executed.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId BackgroundServiceLockAcquired { get; } =
            new EventId(42, Prefix + nameof(BackgroundServiceLockAcquired));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when an error occurs executing the
        ///     <see cref="DistributedBackgroundService" />.
        /// </summary>
        /// <remarks>
        ///     Default log level: Error.
        /// </remarks>
        public static EventId BackgroundServiceException { get; } =
            new EventId(43, Prefix + nameof(BackgroundServiceException));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="RecurringDistributedBackgroundService" /> is stopped.
        /// </summary>
        /// <remarks>
        ///     Default log level: Information.
        /// </remarks>
        public static EventId RecurringBackgroundServiceStopped { get; } =
            new EventId(51, Prefix + nameof(RecurringBackgroundServiceStopped));

        /// <summary>
        ///     Gets the <see cref="EventId" /> of the log that is written when the
        ///     <see cref="RecurringDistributedBackgroundService" /> is sleeping in between the executions.
        /// </summary>
        /// <remarks>
        ///     Default log level: Debug.
        /// </remarks>
        public static EventId RecurringDistributedBackgroundServiceBackgroundServiceSleeping { get; } =
            new EventId(52, Prefix + nameof(RecurringDistributedBackgroundServiceBackgroundServiceSleeping));
    }
}
