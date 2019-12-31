// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Background
{
    public class DistributedLockSettings
    {
        private static readonly TimeSpan DefaultAcquireRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultHeartbeatTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// </summary>
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
        ///     1 minute)
        /// </param>
        /// <param name="heartbeatInterval">The interval at which the heartbeat has to be sent (default is 1 second).</param>
        public DistributedLockSettings(
            TimeSpan? acquireTimeout = null,
            TimeSpan? acquireRetryInterval = null,
            TimeSpan? heartbeatTimeout = null,
            TimeSpan? heartbeatInterval = null)
            : this(
                null,
                null,
                acquireTimeout,
                acquireRetryInterval,
                heartbeatTimeout,
                heartbeatInterval)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="resourceName">The name of the lock / the resource being locked.</param>
        /// <param name="uniqueId">A unique identifier representing the entity trying to acquire the lock (default is a new guid).</param>
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
        ///     1 minute)
        /// </param>
        /// <param name="heartbeatInterval">The interval at which the heartbeat has to be sent (default is 1 second).</param>
        public DistributedLockSettings(
            string resourceName,
            string uniqueId = null,
            TimeSpan? acquireTimeout = null,
            TimeSpan? acquireRetryInterval = null,
            TimeSpan? heartbeatTimeout = null,
            TimeSpan? heartbeatInterval = null)
        {
            if (heartbeatInterval >= heartbeatTimeout)
                throw new ArgumentException("The heartbeat interval must be shorter than the timeout.");

            ResourceName = resourceName;
            UniqueId = uniqueId ?? Guid.NewGuid().ToString("N");
            AcquireTimeout = acquireTimeout;
            AcquireRetryInterval = acquireRetryInterval ?? DefaultAcquireRetryInterval;
            HeartbeatTimeout = heartbeatTimeout ?? DefaultHeartbeatTimeout;
            HeartbeatInterval = heartbeatInterval ?? DefaultHeartbeatInterval;

            FailedHeartbeatsThreshold = Math.Min(0,
                (int) (HeartbeatTimeout.TotalMilliseconds / HeartbeatInterval.TotalMilliseconds) - 1);
        }

        public string ResourceName { get; internal set; }
        public string UniqueId { get; }
        public TimeSpan? AcquireTimeout { get; }
        public TimeSpan AcquireRetryInterval { get; }
        public TimeSpan HeartbeatTimeout { get; }
        public TimeSpan HeartbeatInterval { get; }
        public int FailedHeartbeatsThreshold { get; }
    }
}