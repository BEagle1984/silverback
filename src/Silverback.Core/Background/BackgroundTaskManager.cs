// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Silverback.Background
{
    public class BackgroundTaskManager : IBackgroundTaskManager
    {
        private readonly ConcurrentBag<BackgroundTask> _tasks = new ConcurrentBag<BackgroundTask>();
        private readonly IDistributedLockManager _distributedLockManager;
        private readonly ILoggerFactory _loggerFactory;

        public BackgroundTaskManager(IDistributedLockManager distributedLockManager, ILoggerFactory loggerFactory)
        {
            _distributedLockManager = distributedLockManager;
            _loggerFactory = loggerFactory;
        }

        public void Start(string taskName, Action task, DistributedLockSettings distributedLockSettings = null)
        {
            _tasks.Add(new BackgroundTask(
                taskName, task,
                distributedLockSettings, _distributedLockManager,
                _loggerFactory.CreateLogger<BackgroundTask>()));
        }
    }
}