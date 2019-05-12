// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Silverback.Background
{
    public class BackgroundTask
    {
        private readonly Action _task;
        private readonly DistributedLockSettings _distributedLockSettings;
        private readonly IDistributedLockManager _distributedLockManager;
        private readonly ILogger<BackgroundTask> _logger;
        private IDisposable _acquiredLock;

        public BackgroundTask(string name, Action task, DistributedLockSettings distributedLockSettings, IDistributedLockManager distributedLockManager, ILogger<BackgroundTask> logger)
        {
            _task = task;
            _distributedLockSettings = distributedLockSettings;
            _distributedLockManager = distributedLockManager;
            _logger = logger;
            Name = name;

            RunInBackground();
        }

        private void RunInBackground() => Task.Run(Run);

        public string Name { get; }

        private void Run()
        {
            try
            {
                _acquiredLock = _distributedLockManager?.Acquire(
                    $"Silverback.BackgroundTask.{Name}",
                    _distributedLockSettings);

                _task.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task {taskName} failed. See inner exception for details.", Name);
            }
            finally
            {
                _acquiredLock?.Dispose();
            }
        }
    }
}
