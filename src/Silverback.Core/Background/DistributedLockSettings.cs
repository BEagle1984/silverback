// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Background
{
    public class DistributedLockSettings
    {
        public DistributedLockSettings(TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null)
        {
            AcquireTimeout = acquireTimeout;
            AcquireRetryInterval = acquireRetryInterval;
            HeartbeatTimeout = heartbeatTimeout;
        }

        public TimeSpan? AcquireTimeout { get; set; }

        public TimeSpan? AcquireRetryInterval { get; set; }

        public TimeSpan? HeartbeatTimeout { get; set; }
    }
}