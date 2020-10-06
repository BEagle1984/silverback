// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    internal class DefaultSequenceStore : ISequenceStore
    {
        private readonly Dictionary<object, ISequence> _store = new Dictionary<object, ISequence>();

        public bool HasPendingSequences => _store.Any(sequence => !sequence.Value.IsComplete);

        public Task<TSequence?> GetAsync<TSequence>(object sequenceId)
            where TSequence : class, ISequence
        {
            _store.TryGetValue(sequenceId, out var sequence);

            if (sequence is Sequence sequenceImpl)
                sequenceImpl.IsNew = false;

            return Task.FromResult((TSequence?)sequence);
        }

        public async Task<TSequence> AddAsync<TSequence>(TSequence sequence)
            where TSequence : class, ISequence
        {
            Check.NotNull(sequence, nameof(sequence));

            if (_store.TryGetValue(sequence.SequenceId, out var oldSequence))
                await oldSequence.AbortAsync().ConfigureAwait(false);

            _store[sequence.SequenceId] = sequence;

            return sequence;
        }

        public Task RemoveAsync(object sequenceId)
        {
            _store.Remove(sequenceId);
            return Task.CompletedTask;
        }
    }
}
