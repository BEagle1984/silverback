// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Sequences.Chunking;

/// <inheritdoc cref="IChunkEnricherFactory" />
public class ChunkEnricherFactory : TypeBasedExtensibleFactory<IChunkEnricher, ProducerEndpointConfiguration>, IChunkEnricherFactory
{
    /// <inheritdoc cref="IChunkEnricherFactory.GetEnricher" />
    public IChunkEnricher GetEnricher(ProducerEndpointConfiguration endpointConfiguration, IServiceProvider serviceProvider) =>
        GetService(endpointConfiguration, serviceProvider) ?? NullChunkEnricher.Instance;
}
