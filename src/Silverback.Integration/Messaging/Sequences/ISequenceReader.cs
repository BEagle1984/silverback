// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    public interface ISequenceReader
    {
        IRawInboundEnvelope? SetSequence(IRawInboundEnvelope rawInboundEnvelope);

    }
}
