// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the specific implementation of the <see cref="KafkaOffsetStoreSettings" />.
/// </summary>
public interface IKafkaOffsetStoreSettingsImplementationBuilder
{
    /// <summary>
    ///     Builds the settings instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaOffsetStoreSettings" />.
    /// </returns>
    public KafkaOffsetStoreSettings Build();
}
