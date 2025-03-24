// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Lock;

/// <summary>
///     The <see cref="DistributedLock" /> settings.
/// </summary>
public abstract record TableBasedDistributedLockSettings : DistributedLockSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TableBasedDistributedLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    protected TableBasedDistributedLockSettings(string lockName)
        : base(lockName)
    {
    }

    /// <summary>
    ///     Gets the interval between two attempts to acquire the lock. The default is 1 second.
    /// </summary>
    public TimeSpan AcquireInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Gets the interval between two heartbeat updates. The default is 500 milliseconds.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Gets the maximum duration between two heartbeat updates, after which the lock is considered lost. The default is 5 seconds.
    /// </summary>
    public TimeSpan LockTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <inheritdoc cref="DistributedLockSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (AcquireInterval <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The acquire interval must be greater than zero.");

        if (HeartbeatInterval <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The heartbeat interval must be greater than zero.");

        if (LockTimeout <= TimeSpan.Zero)
            throw new SilverbackConfigurationException("The lock timeout must be greater than zero.");

        if (LockTimeout <= HeartbeatInterval)
            throw new SilverbackConfigurationException("The lock timeout must be greater than the heartbeat interval.");
    }
}
