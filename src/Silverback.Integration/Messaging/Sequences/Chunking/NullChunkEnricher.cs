// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences.Chunking;

internal sealed class NullChunkEnricher : IChunkEnricher
{
    public static readonly IChunkEnricher Instance = new NullChunkEnricher();

    private NullChunkEnricher()
    {
    }

    public MessageHeader? GetFirstChunkMessageHeader(IOutboundEnvelope envelope) => null;
}
