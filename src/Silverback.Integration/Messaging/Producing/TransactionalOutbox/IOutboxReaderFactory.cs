// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Builds an <see cref="IOutboxReader" /> instance according to the provided <see cref="OutboxSettings" />.
/// </summary>
public interface IOutboxReaderFactory
{
    /// <summary>
    ///     Returns an <see cref="IOutboxReader" /> according to the specified settings.
    /// </summary>
    /// <param name="settings">
    ///     The settings that will be used to create the <see cref="IOutboxReader" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboxReader" />.
    /// </returns>
    IOutboxReader GetReader(OutboxSettings settings);
}
