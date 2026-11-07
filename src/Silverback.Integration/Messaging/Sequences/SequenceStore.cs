// Copyright (c) 2026 Sergio Aquilini
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

internal sealed class SequenceStore : ISequenceStore
{
    private readonly Guid _id = Guid.NewGuid();

    // Locked because the store is also accessed outside the consume loop (e.g., when stopping or aborting), and enumerated through
    // a snapshot, since a sequence can be added or removed while it is enumerated
    private readonly Dictionary<string, ISequence> _store = [];

    private readonly System.Threading.Lock _storeLock = new();

    private readonly ISilverbackLogger _logger;

    public SequenceStore(ISilverbackLogger logger)
    {
        _logger = logger;
    }

    public int Count
    {
        get
        {
            lock (_storeLock)
            {
                return _store.Count;
            }
        }
    }

    public bool IsDisposed { get; private set; }

    public ValueTask<TSequence?> GetAsync<TSequence>(string sequenceId, bool matchPrefix = false)
        where TSequence : class, ISequence
    {
        Check.ThrowObjectDisposedIf(IsDisposed, this);

        ISequence? sequence;

        lock (_storeLock)
        {
            if (matchPrefix)
            {
                sequence = _store.FirstOrDefault(keyValuePair => keyValuePair.Key.StartsWith(sequenceId, StringComparison.Ordinal)).Value;
            }
            else
            {
                _store.TryGetValue(sequenceId, out sequence);
            }
        }

        if (sequence is ISequenceImplementation sequenceImpl)
            sequenceImpl.SetIsNew(false);

        return ValueTask.FromResult((TSequence?)sequence);
    }

    public async ValueTask<TSequence> AddAsync<TSequence>(TSequence sequence)
        where TSequence : class, ISequence
    {
        Check.NotNull(sequence, nameof(sequence));
        Check.ThrowObjectDisposedIf(IsDisposed, this);

        _logger.LogSequenceAddedToStore(sequence, _id);

        ISequence? oldSequence;

        lock (_storeLock)
        {
            _store.TryGetValue(sequence.SequenceId, out oldSequence);
        }

        if (oldSequence != null)
            await oldSequence.AbortAsync(SequenceAbortReason.IncompleteSequence).ConfigureAwait(false);

        lock (_storeLock)
        {
            _store[sequence.SequenceId] = sequence;
        }

        return sequence;
    }

    public ValueTask RemoveAsync(string sequenceId)
    {
        _logger.LogSequenceRemovedFromStore(sequenceId, _id);

        lock (_storeLock)
        {
            _store.Remove(sequenceId);
        }

        return default;
    }

    public IReadOnlyCollection<ISequence> GetPendingSequences(bool includeUnbounded = false, bool includeChildren = false) =>
    [
        .. GetSnapshot().Where(sequence => sequence.IsPending &&
                                           (includeUnbounded || sequence is not UnboundedSequence) &&
                                           (includeChildren || sequence.ParentSequence == null))
    ];

    public IEnumerator<ISequence> GetEnumerator() => ((IEnumerable<ISequence>)GetSnapshot()).GetEnumerator();

    public void Dispose() => DisposeAsync().SafeWait();

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;

        _logger.LogSequenceStoreDisposing(_id);

        await GetSnapshot().AbortAllAsync(SequenceAbortReason.Disposing).ConfigureAwait(false);

        IsDisposed = true;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private ISequence[] GetSnapshot()
    {
        lock (_storeLock)
        {
            return [.. _store.Values];
        }
    }
}
