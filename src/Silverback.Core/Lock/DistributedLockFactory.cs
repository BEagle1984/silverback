// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.ExtensibleFactories;

namespace Silverback.Lock;

/// <inheritdoc cref="IDistributedLockFactory" />
public class DistributedLockFactory : ExtensibleFactory<IDistributedLock, DistributedLockSettings>, IDistributedLockFactory
{
    /// <inheritdoc cref="IDistributedLockFactory.GetDistributedLock" />
    public IDistributedLock GetDistributedLock(DistributedLockSettings? settings) =>
        settings == null ? NullLock.Instance : GetService(settings);
}
