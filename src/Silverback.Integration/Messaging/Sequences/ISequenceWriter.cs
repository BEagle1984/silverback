// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    public interface ISequenceWriter
    {
        Task<IAsyncEnumerable<IRawOutboundEnvelope>?> CreateSequence(IRawOutboundEnvelope envelope);
    }
}
