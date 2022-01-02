// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

/// <summary>
///     The <see cref="DistributedLock" /> settings.
/// </summary>
public abstract record DistributedLockSettings
{
    /// <summary>
    ///     Gets the name of the lock.
    /// </summary>
    public string LockName { get; init; } = string.Empty;
}
