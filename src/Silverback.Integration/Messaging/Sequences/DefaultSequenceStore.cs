// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    internal sealed class DefaultSequenceStore : ISequenceStore
    {
        private readonly Guid _id = Guid.NewGuid();

        private readonly Dictionary<string, ISequence> _store = new();

        private readonly ISilverbackLogger<DefaultSequenceStore> _logger;

        private bool _disposed;

        public DefaultSequenceStore(ISilverbackLogger<DefaultSequenceStore> logger)
        {
            _logger = logger;
        }

        public int Count => _store.Count;

        public Task<TSequence?> GetAsync<TSequence>(string sequenceId, bool matchPrefix = false)
            where TSequence : class, ISequence
        {
            ISequence? sequence;

            if (matchPrefix)
            {
                sequence = _store.Values.FirstOrDefault(
                    storedSequence =>
                        storedSequence.SequenceId.StartsWith(sequenceId, StringComparison.Ordinal));
            }
            else
            {
                _store.TryGetValue(sequenceId, out sequence);
            }

            if (sequence is ISequenceImplementation sequenceImpl)
                sequenceImpl.SetIsNew(false);

            return Task.FromResult((TSequence?)sequence);
        }

        public async Task<TSequence> AddAsync<TSequence>(TSequence sequence)
            where TSequence : class, ISequence
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            Check.NotNull(sequence, nameof(sequence));

            _logger.LogLowLevelTrace(
                "Adding {sequenceType} '{sequenceId}' to store '{sequenceStoreId}'.",
                () => new object[]
                {
                    sequence.GetType().Name,
                    sequence.SequenceId,
                    _id
                });

            if (_store.TryGetValue(sequence.SequenceId, out var oldSequence))
                await oldSequence.AbortAsync(SequenceAbortReason.IncompleteSequence).ConfigureAwait(false);

            _store[sequence.SequenceId] = sequence;

            return sequence;
        }

        public Task RemoveAsync(string sequenceId)
        {
            _logger.LogLowLevelTrace(
                "Removing sequence '{sequenceId}' from store '{sequenceStoreId}'.",
                () => new object[]
                {
                    sequenceId,
                    _id
                });

            _store.Remove(sequenceId);
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<ISequence> GetPendingSequences(bool includeUnbounded = false) =>
            _store.Values.Where(
                    sequence =>
                        sequence.IsPending && (includeUnbounded || sequence is not UnboundedSequence))
                .ToList();

        public IEnumerator<ISequence> GetEnumerator() => _store.Values.GetEnumerator();

        public void Dispose() => AsyncHelper.RunValueTaskSynchronously(DisposeAsync);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _logger.LogLowLevelTrace("Disposing sequence store {sequenceStoreId}", () => new object[] { _id });

            await _store.Values.AbortAllAsync(SequenceAbortReason.Disposing).ConfigureAwait(false);

            _disposed = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
