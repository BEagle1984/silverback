// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Lock;

/// <summary>
///     The <see cref="DistributedLock" /> settings.
/// </summary>
public abstract record DistributedLockSettings : IValidatableSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DistributedLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    protected DistributedLockSettings(string lockName)
    {
        LockName = lockName;
    }

    /// <summary>
    ///     Gets the name of the lock.
    /// </summary>
    public string LockName { get; }

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public virtual void Validate()
    {
        if (string.IsNullOrWhiteSpace(LockName))
            throw new SilverbackConfigurationException("The lock name is required.");
    }
}
