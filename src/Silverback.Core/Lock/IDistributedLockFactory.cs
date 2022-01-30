// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

/// <summary>
///     Builds an <see cref="IDistributedLock" /> instance according to the provided <see cref="DistributedLockSettings" />.
/// </summary>
public interface IDistributedLockFactory
{
    /// <summary>
    ///     Returns an <see cref="IDistributedLock" /> according to the specified settings.
    /// </summary>
    /// <param name="settings">
    ///     The settings that will be used to create the <see cref="IDistributedLock" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IDistributedLock" />.
    /// </returns>
    IDistributedLock GetDistributedLock(DistributedLockSettings? settings);
}
