// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Util;

namespace Silverback.Messaging.Sequences;

internal sealed class DefaultSequenceStore : ISequenceStore
{
    private readonly Guid _id = Guid.NewGuid();

    private readonly Dictionary<string, ISequence> _store = new();

    private readonly ISilverbackLogger<DefaultSequenceStore> _logger;

    public DefaultSequenceStore(ISilverbackLogger<DefaultSequenceStore> logger)
    {
        _logger = logger;
    }

    public int Count => _store.Count;

    public bool Disposed { get; private set; }

    public ValueTask<TSequence?> GetAsync<TSequence>(string sequenceId, bool matchPrefix = false)
        where TSequence : class, ISequence
    {
        if (Disposed)
            throw new ObjectDisposedException(GetType().FullName);

        ISequence? sequence;

        if (matchPrefix)
        {
            sequence = _store.FirstOrDefault(keyValuePair => keyValuePair.Key.StartsWith(sequenceId, StringComparison.Ordinal)).Value;
        }
        else
        {
            _store.TryGetValue(sequenceId, out sequence);
        }

        if (sequence is ISequenceImplementation sequenceImpl)
            sequenceImpl.SetIsNew(false);

        return ValueTaskFactory.FromResult((TSequence?)sequence);
    }

    public async ValueTask<TSequence> AddAsync<TSequence>(TSequence sequence)
        where TSequence : class, ISequence
    {
        Check.NotNull(sequence, nameof(sequence));

        if (Disposed)
            throw new ObjectDisposedException(GetType().FullName);

        _logger.LogLowLevelTrace(
            "Adding {sequenceType} '{sequenceId}' to store '{sequenceStoreId}'.",
            () => new object[]
            {
                sequence.GetType().Name,
                sequence.SequenceId,
                _id
            });

        if (_store.TryGetValue(sequence.SequenceId, out ISequence? oldSequence))
            await oldSequence.AbortAsync(SequenceAbortReason.IncompleteSequence).ConfigureAwait(false);

        _store[sequence.SequenceId] = sequence;

        return sequence;
    }

    public ValueTask RemoveAsync(string sequenceId)
    {
        _logger.LogLowLevelTrace(
            "Removing sequence '{sequenceId}' from store '{sequenceStoreId}'.",
            () => new object[]
            {
                sequenceId,
                _id
            });

        _store.Remove(sequenceId);
        return default;
    }

    public IReadOnlyCollection<ISequence> GetPendingSequences(bool includeUnbounded = false) =>
        _store.Values.Where(sequence => sequence.IsPending && (includeUnbounded || sequence is not UnboundedSequence)).ToList();

    public IEnumerator<ISequence> GetEnumerator() => _store.Values.GetEnumerator();

    public void Dispose() => AsyncHelper.RunSynchronously(DisposeAsync);

    public async ValueTask DisposeAsync()
    {
        if (Disposed)
            return;

        _logger.LogLowLevelTrace("Disposing sequence store {sequenceStoreId}", () => new object[] { _id });

        await _store.Values.AbortAllAsync(SequenceAbortReason.Disposing).ConfigureAwait(false);

        Disposed = true;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
