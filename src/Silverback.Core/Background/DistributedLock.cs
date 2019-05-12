// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Background
{
    public class DistributedLock : IDisposable
    {
        private readonly string _name;
        private readonly IDistributedLockManager _lockManager;
        private bool _disposed;

        public DistributedLock(string name, IDistributedLockManager lockManager)
        {
            _name = name;
            _lockManager = lockManager;

            Task.Run(SendHeartbeats);
        }

        private void SendHeartbeats()
        {
            while (!_disposed)
            {
                _lockManager.SendHeartbeat(_name);

                Thread.Sleep(1000);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _lockManager.Release(_name);
        }
    }
}