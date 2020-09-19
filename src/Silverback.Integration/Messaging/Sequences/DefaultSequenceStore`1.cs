// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    internal class DefaultSequenceStore<TSequence> : ISequenceStore<TSequence>
        where TSequence : class, ISequence
    {
        private readonly ConcurrentDictionary<object, TSequence> _store = new ConcurrentDictionary<object, TSequence>();

        public TSequence? Get(object sequenceId)
        {
            _store.TryGetValue(sequenceId, out var result);
            return result;
        }

        public TSequence Add(TSequence sequence)
        {
            Check.NotNull(sequence, nameof(sequence));

            _store.TryAdd(sequence.SequenceId, sequence);

            return sequence;
        }

        public void Remove(object sequenceId)
        {
            _store.TryRemove(sequenceId, out _);
        }
    }
}
