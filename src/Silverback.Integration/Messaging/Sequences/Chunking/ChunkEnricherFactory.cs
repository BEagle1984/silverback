// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.ExtensibleFactories;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     Enriches the produced chunks adding some additional broker-specific headers.
/// </summary>
public class ChunkEnricherFactory : TypeBasedExtensibleFactory<IChunkEnricher, ProducerEndpoint>, IChunkEnricherFactory
{
    /// <inheritdoc cref="IChunkEnricherFactory.GetEnricher" />
    public IChunkEnricher GetEnricher(ProducerEndpoint endpoint) =>
        GetService(endpoint) ?? NullChunkEnricher.Instance;
}
