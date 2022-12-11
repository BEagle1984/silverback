// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.ExtensibleFactories;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <inheritdoc cref="IKafkaOffsetStoreFactory" />
public class KafkaOffsetStoreFactory : ExtensibleFactory<IKafkaOffsetStore, KafkaOffsetStoreSettings>, IKafkaOffsetStoreFactory
{
    /// <inheritdoc cref="IKafkaOffsetStoreFactory.GetStore" />
    public IKafkaOffsetStore GetStore(KafkaOffsetStoreSettings settings) => GetService(settings);
}
