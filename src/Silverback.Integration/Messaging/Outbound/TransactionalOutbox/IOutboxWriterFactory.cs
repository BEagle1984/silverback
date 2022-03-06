// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     Builds an <see cref="IOutboxWriter" /> instance according to the provided <see cref="OutboxSettings" />.
/// </summary>
public interface IOutboxWriterFactory
{
    /// <summary>
    ///     Returns an <see cref="IOutboxWriter" /> according to the specified settings.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <param name="settings">
    ///     The settings that will be used to create the <see cref="IOutboxWriter" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboxWriter" />.
    /// </returns>
    IOutboxWriter GetWriter<TSettings>(TSettings? settings)
        where TSettings : OutboxSettings;
}
