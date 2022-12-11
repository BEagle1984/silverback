// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
    /// <returns>
    ///     The <see cref="IKafkaOffsetStore" />.
    /// </returns>
    IKafkaOffsetStore GetStore(KafkaOffsetStoreSettings settings);
}
