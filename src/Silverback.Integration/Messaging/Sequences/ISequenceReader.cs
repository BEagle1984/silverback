// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    public interface ISequenceReader
    {
        bool CanHandleSequence(IRawInboundEnvelope envelope);

        Task<ISequence?> HandleSequence(IRawInboundEnvelope envelope);
    }
}
