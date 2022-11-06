// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;
using Silverback.Lock;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="IOutboxWriter" /> and <see cref="IOutboxReader" /> settings.
/// </summary>
public abstract record OutboxSettings : IValidatableSettings
{
    /// <summary>
    ///     Returns an instance of <see cref="DistributedLockSettings" /> configured to work with the same resource (e.g. the same database).
    /// </summary>
    /// <returns>
    ///     The <see cref="DistributedLockSettings" />, if a compatible distributed lock implementation exists.
    /// </returns>
    public abstract DistributedLockSettings? GetCompatibleLockSettings();

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public virtual void Validate()
    {
    }
}
