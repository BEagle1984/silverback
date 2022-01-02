// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Lock;

public class DistributedLockSettingsBuilder : IDistributedLockSettingsBuilder
{
    public DistributedLockSettings Build() =>
        throw new InvalidOperationException(
            "The DistributedLock wasn't configured. " +
            "Please choose an actual implementation.");
}

public interface IDistributedLockSettingsBuilder
{
    DistributedLockSettings Build();
}
