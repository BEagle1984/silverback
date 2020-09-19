// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    public interface ISequenceWriter
    {
        bool MustCreateSequence(IOutboundEnvelope envelope);

        IAsyncEnumerable<IOutboundEnvelope> CreateSequence(IOutboundEnvelope envelope);
    }
}
