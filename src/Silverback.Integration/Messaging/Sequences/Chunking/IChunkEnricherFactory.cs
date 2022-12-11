// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     Builds an <see cref="IChunkEnricher" /> instance for the <see cref="ProducerEndpoint" />.
/// </summary>
public interface IChunkEnricherFactory
{
    /// <summary>
    ///     Returns an <see cref="IChunkEnricher" /> according to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">
    ///     The endpoint that will be used to create the <see cref="IChunkEnricher" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IChunkEnricher" />.
    /// </returns>
    IChunkEnricher GetEnricher(ProducerEndpoint endpoint);
}
