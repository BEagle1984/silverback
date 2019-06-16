// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Background
{
    public class DistributedLock
    {
        private readonly string _name;
        private readonly IDistributedLockManager _lockManager;
        private readonly int _heartbeatIntervalInMilliseconds;
        private bool _released;

        public DistributedLock(string name, IDistributedLockManager lockManager, int heartbeatIntervalInMilliseconds = 1000)
        {
            _name = name;
            _lockManager = lockManager;
            _heartbeatIntervalInMilliseconds = heartbeatIntervalInMilliseconds;

            Task.Run(SendHeartbeats);
        }

        private async Task SendHeartbeats()
        {
            while (!_released)
            {
                await _lockManager.SendHeartbeat(_name);

                await Task.Delay(_heartbeatIntervalInMilliseconds);
            }
        }

        public async Task Release()
        {
            _released = true;
            await _lockManager.Release(_name);
        }
    }
}