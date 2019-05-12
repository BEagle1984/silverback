// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Background
{
    public interface IBackgroundTaskManager
    {
        void Start(string taskName, Action task, DistributedLockSettings distributedLockSettings = null);
    }
}