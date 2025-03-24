// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <inheritdoc cref="IKafkaOffsetStoreFactory" />
public class KafkaOffsetStoreFactory : ExtensibleFactory<IKafkaOffsetStore, KafkaOffsetStoreSettings>, IKafkaOffsetStoreFactory
{
    /// <inheritdoc cref="IKafkaOffsetStoreFactory.GetStore" />
    public IKafkaOffsetStore GetStore(KafkaOffsetStoreSettings settings, IServiceProvider serviceProvider) =>
        GetService(settings, serviceProvider);
}
