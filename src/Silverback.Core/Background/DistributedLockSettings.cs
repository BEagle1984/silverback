// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Background
{
    public class DistributedLockSettings
    {
        public DistributedLockSettings(string resourceName = null, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null)
        {
            ResourceName = resourceName;
            AcquireTimeout = acquireTimeout;
            AcquireRetryInterval = acquireRetryInterval;
            HeartbeatTimeout = heartbeatTimeout;
        }

        public string ResourceName { get; set; }

        public TimeSpan? AcquireTimeout { get; set; }

        public TimeSpan? AcquireRetryInterval { get; set; }

        public TimeSpan? HeartbeatTimeout { get; set; }
    }
}