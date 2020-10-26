// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    internal sealed class DefaultSequenceStore : ISequenceStore
    {
        private readonly Dictionary<string, ISequence> _store = new Dictionary<string, ISequence>();

        public bool HasPendingSequences =>
            _store.Any(sequencePair => sequencePair.Value.IsPending);

        public int Count => _store.Count;

        public Task<TSequence?> GetAsync<TSequence>(string sequenceId)
            where TSequence : class, ISequence
        {
            _store.TryGetValue(sequenceId, out var sequence);

            if (sequence is ISequenceImplementation sequenceImpl)
                sequenceImpl.SetIsNew(false);

            return Task.FromResult((TSequence?)sequence);
        }

        public async Task<TSequence> AddAsync<TSequence>(TSequence sequence)
            where TSequence : class, ISequence
        {
            Check.NotNull(sequence, nameof(sequence));

            if (_store.TryGetValue(sequence.SequenceId, out var oldSequence))
                await oldSequence.AbortAsync(SequenceAbortReason.IncompleteSequence).ConfigureAwait(false);

            _store[sequence.SequenceId] = sequence;

            return sequence;
        }

        public Task RemoveAsync(string sequenceId)
        {
            _store.Remove(sequenceId);
            return Task.CompletedTask;
        }

        public IEnumerator<ISequence> GetEnumerator() => _store.Values.GetEnumerator();

        public void Dispose()
        {
            AsyncHelper.RunSynchronously(
                () => _store.Values
                    .ForEachAsync(sequence => sequence.AbortAsync(SequenceAbortReason.Disposing)));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
