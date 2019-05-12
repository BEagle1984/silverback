// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Background
{
    public class NullLockManager : IDistributedLockManager
    {
        public IDisposable Acquire(string resourceName, DistributedLockSettings settings = null) => null;

        public IDisposable Acquire(string resourceName, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null) => null;

        public void SendHeartbeat(string resourceName) { }

        public void Release(string resourceName) { }
    }
}