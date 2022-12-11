// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

/// <summary>
///     The <see cref="InMemoryLock" /> settings.
/// </summary>
public record InMemoryLockSettings : DistributedLockSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    public InMemoryLockSettings(string lockName)
        : base(lockName)
    {
    }
}
