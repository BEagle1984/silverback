// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Lock;

namespace Silverback.Configuration;

/// <summary>
///     Builds the specific implementation of the <see cref="DistributedLockSettings" />.
/// </summary>
public interface IDistributedLockSettingsImplementationBuilder
{
    /// <summary>
    ///     Builds the settings instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="DistributedLockSettings" />.
    /// </returns>
    public DistributedLockSettings Build();
}
