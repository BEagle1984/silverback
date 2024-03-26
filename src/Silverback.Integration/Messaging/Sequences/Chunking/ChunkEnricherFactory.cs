// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.ExtensibleFactories;

namespace Silverback.Messaging.Sequences.Chunking;

/// <inheritdoc cref="IChunkEnricherFactory" />
public class ChunkEnricherFactory : TypeBasedExtensibleFactory<IChunkEnricher, ProducerEndpoint>, IChunkEnricherFactory
{
    /// <inheritdoc cref="IChunkEnricherFactory.GetEnricher" />
    public IChunkEnricher GetEnricher(ProducerEndpoint endpoint, IServiceProvider serviceProvider) =>
        GetService(endpoint, serviceProvider) ?? NullChunkEnricher.Instance;
}
