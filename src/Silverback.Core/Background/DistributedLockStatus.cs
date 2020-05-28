// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Background
{
    /// <summary>
    ///     Specifies the current status of the <see cref="DistributedLock" />.
    /// </summary>
    public enum DistributedLockStatus
    {
        /// <summary>
        ///     The lock has been acquired.
        /// </summary>
        Acquired,

        /// <summary>
        ///     The previously acquired lock has been lost for some reason (such as failure to send the heartbeat).
        /// </summary>
        Lost,

        /// <summary>
        ///     The lock has been released.
        /// </summary>
        Released
    }
}
