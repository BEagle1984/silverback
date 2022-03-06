// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound.TransactionalOutbox;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the specific implementation of the <see cref="OutboxSettings" />.
/// </summary>
public interface IOutboxSettingsImplementationBuilder
{
    /// <summary>
    ///     Builds the settings instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="OutboxSettings" />.
    /// </returns>
    public OutboxSettings Build();
}
