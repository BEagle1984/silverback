// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Lock;

/// <inheritdoc cref="IDistributedLockFactory" />
public class DistributedLockFactory : ExtensibleFactory<IDistributedLock, DistributedLockSettings>, IDistributedLockFactory
{
    /// <inheritdoc cref="IDistributedLockFactory.GetDistributedLock{TSettings}" />
    public IDistributedLock GetDistributedLock<TSettings>(TSettings? settings)
        where TSettings : DistributedLockSettings =>
        GetService(settings) ?? NullLock.Instance;
}
