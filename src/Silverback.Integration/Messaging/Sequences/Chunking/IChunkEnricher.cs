// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     Enriches the produced chunks adding some additional broker-specific headers.
/// </summary>
public interface IChunkEnricher
{
    /// <summary>
    ///     Gets an optional header to be appended to each chunk, pointing to the first message in the sequence.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the first chunk.
    /// </param>
    /// <returns>
    ///     An optional header to be appended to all subsequent chunks.
    /// </returns>
    MessageHeader? GetFirstChunkMessageHeader(IOutboundEnvelope envelope);
}
