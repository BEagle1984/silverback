// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Builds an <see cref="IOutboxWriter" /> instance according to the provided <see cref="OutboxSettings" />.
/// </summary>
public interface IOutboxWriterFactory
{
    /// <summary>
    ///     Returns an <see cref="IOutboxWriter" /> according to the specified settings.
    /// </summary>
    /// <param name="settings">
    ///     The settings that will be used to create the <see cref="IOutboxWriter" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> that can be used to resolve additional services.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboxWriter" />.
    /// </returns>
    IOutboxWriter GetWriter(OutboxSettings settings, IServiceProvider serviceProvider);
}
