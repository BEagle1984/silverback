// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Background
{
    public interface IDistributedLockManager
    {
        IDisposable Acquire(string resourceName, DistributedLockSettings settings = null);

        IDisposable Acquire(string resourceName, TimeSpan? acquireTimeout = null, TimeSpan? acquireRetryInterval = null, TimeSpan? heartbeatTimeout = null);

        void SendHeartbeat(string resourceName);

        void Release(string resourceName);
    }
}