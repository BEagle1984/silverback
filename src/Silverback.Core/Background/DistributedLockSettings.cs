// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;

namespace Silverback.Background
{
    /// <summary>
    ///     The settings to be applied to the configured <see cref="IDistributedLockManager" />.
    /// </summary>
    public class DistributedLockSettings
    {
        private static readonly TimeSpan DefaultAcquireRetryInterval = TimeSpan.FromSeconds(30);

        private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(1);

        private static readonly TimeSpan DefaultHeartbeatTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedLockSettings" /> class.
        /// </summary>
        /// <param name="resourceName">
        ///     The name of the lock / the resource being locked.
        /// </param>
        /// <param name="uniqueId">
        ///     A unique identifier representing the entity trying to acquire the lock (default is a new
        ///     <see cref="System.Guid" />).
        /// </param>
        /// <param name="acquireTimeout">
        ///     After the acquire timeout is expired the lock manager will abort the lock acquisition
        ///     (default is no timeout).
        /// </param>
        /// <param name="acquireRetryInterval">
        ///     The interval at which the lock manager checks if a lock can be acquired for the
        ///     specified resource (default is 30 seconds).
        /// </param>
        /// <param name="heartbeatTimeout">
        ///     After the heartbeat timeout is expired the lock will be considered released (default is
        ///     1 minute).
        /// </param>
        /// <param name="heartbeatInterval">
        ///     The interval at which the heartbeat has to be sent (default is 1 second).
        /// </param>
        public DistributedLockSettings(
            string resourceName = "",
            string? uniqueId = null,
            TimeSpan? acquireTimeout = null,
            TimeSpan? acquireRetryInterval = null,
            TimeSpan? heartbeatTimeout = null,
            TimeSpan? heartbeatInterval = null)
        {
            if (heartbeatInterval >= heartbeatTimeout)
                throw new ArgumentException("The heartbeat interval must be shorter than the timeout.");

            ResourceName = resourceName ?? throw new ArgumentNullException(nameof(resourceName));
            UniqueId = uniqueId ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            AcquireTimeout = acquireTimeout;
            AcquireRetryInterval = acquireRetryInterval ?? DefaultAcquireRetryInterval;
            HeartbeatTimeout = heartbeatTimeout ?? DefaultHeartbeatTimeout;
            HeartbeatInterval = heartbeatInterval ?? DefaultHeartbeatInterval;

            int heartbeatsBeforeTimeout =
                (int)(HeartbeatTimeout.TotalMilliseconds / HeartbeatInterval.TotalMilliseconds) - 1;

            FailedHeartbeatsThreshold = Math.Min(0, heartbeatsBeforeTimeout);
        }

        /// <summary>
        ///     Gets the <see cref="DistributedLockSettings" /> signaling that no lock is to be checked and
        ///     acquired. Corresponds to an instance of <see cref="NullLockSettings" />.
        /// </summary>
        public static DistributedLockSettings NoLock { get; } = new NullLockSettings();

        /// <summary>
        ///     Gets the name of the lock / the resource being locked.
        /// </summary>
        public string ResourceName { get; private set; }

        /// <summary>
        ///     Gets a unique identifier representing the entity trying to acquire the lock.
        /// </summary>
        public string UniqueId { get; }

        /// <summary>
        ///     Gets the timeout after which the lock manager will abort the lock acquisition.
        /// </summary>
        public TimeSpan? AcquireTimeout { get; }

        /// <summary>
        ///     Gets the interval at which the lock manager checks if a lock can be acquired for the specified
        ///     resource.
        /// </summary>
        public TimeSpan AcquireRetryInterval { get; }

        /// <summary>
        ///     Gets the timeout after which the lock will be considered released if no heartbeat is sent.
        /// </summary>
        public TimeSpan HeartbeatTimeout { get; }

        /// <summary>
        ///     Gets the interval at which the heartbeat has to be sent.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; }

        /// <summary>
        ///     Gets the maximum number of heartbeats that can be failed to be sent before stopping.
        /// </summary>
        public int FailedHeartbeatsThreshold { get; }

        internal void EnsureResourceNameIsSet(string fallback)
        {
            if (string.IsNullOrEmpty(ResourceName))
                ResourceName = fallback;
        }
    }
}
