// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;

namespace Silverback.Lock;

/// <inheritdoc cref="IDistributedLockFactory" />
public class DistributedLockFactory : ExtensibleFactory<IDistributedLock, DistributedLockSettings>, IDistributedLockFactory
{
    /// <inheritdoc cref="IDistributedLockFactory.GetDistributedLock" />
    public IDistributedLock GetDistributedLock(DistributedLockSettings? settings, IServiceProvider serviceProvider) =>
        settings == null ? NullLock.Instance : GetService(settings, serviceProvider);
}
