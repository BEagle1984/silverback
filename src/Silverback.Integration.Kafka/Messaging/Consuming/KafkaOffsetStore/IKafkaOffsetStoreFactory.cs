// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Builds an <see cref="IKafkaOffsetStore" /> instance according to the provided <see cref="KafkaOffsetStoreSettings" />.
/// </summary>
public interface IKafkaOffsetStoreFactory
{
    /// <summary>
    ///     Returns an <see cref="IKafkaOffsetStore" /> according to the specified settings.
    /// </summary>
    /// <param name="settings">
    ///     The settings that will be used to create the <see cref="IKafkaOffsetStore" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> that can be used to resolve additional services.
    /// </param>
    /// <returns>
    ///     The <see cref="IKafkaOffsetStore" />.
    /// </returns>
    IKafkaOffsetStore GetStore(KafkaOffsetStoreSettings settings, IServiceProvider serviceProvider);
}
