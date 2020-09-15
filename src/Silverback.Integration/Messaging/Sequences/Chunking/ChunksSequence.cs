// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    public class ChunksSequence : Sequence
    {
        private int? _lastIndex;

        public ChunksSequence(object sequenceId, int length)
            : base(sequenceId)
        {
            Length = length;
        }

        public Task AddAsync(int index, IRawInboundEnvelope envelope)
        {
            EnsureOrdering(index);

            return AddAsync(envelope);
        }

        private void EnsureOrdering(int index)
        {
            if (_lastIndex == null && index != 0)
            {
                throw new InvalidOperationException(
                    $"Sequence error. Received chunk with index {index} as first chunk for the sequence {SequenceId}, expected index 0. "); // TODO: Use custom exception type?
            }

            if (_lastIndex != null && index != _lastIndex + 1)
            {
                throw new InvalidOperationException(
                    $"Sequence error. Received chunk with index {index} after index {_lastIndex}."); // TODO: Use custom exception type?
            }

            _lastIndex = index;
        }
    }
}
