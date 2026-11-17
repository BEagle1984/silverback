// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Sequences;

internal static class SequenceEnumerableExtensions
{
    public static ValueTask AbortAllAsync(this IEnumerable<ISequence> sequences, SequenceAbortReason abortReason) =>
        sequences.ParallelForEachAsync(async sequence =>
        {
            if (sequence.IsPending)
                await sequence.AbortAsync(abortReason).ConfigureAwait(false);

            await sequence.AwaitProcessingAsync(false).ConfigureAwait(false);
        });

    // The reader may be blocked pushing a message to a pending child sequence (e.g., the next chunk of a binary message added to a batch),
    // which only the subscriber of its parent reads: if that subscriber moved on without reading it and waits for the next message of the
    // parent, the push never completes and the reader never stops. The parents of the pending child sequences are therefore aborted as soon
    // as the reading is canceled, as the stopped reader would abort all the pending sequences (see AbortAllAsync), aborting their child
    // sequences with them and releasing the push. A parent being completed isn't aborted: its pending child sequences are aborted when it's
    // disposed, once its subscribers returned and it was committed (see SequenceBase). A pending sequence linked to its parent after this
    // snapshot is aborted with it by the sequencer instead (see SequencerConsumerBehaviorBase).
    public static ValueTask AbortParentsOfPendingSequencesAsync(this IEnumerable<ISequence> sequences, SequenceAbortReason abortReason) =>
        sequences
            .Where(sequence => sequence.IsPending)
            .Select(sequence => sequence.ParentSequence)
            .OfType<ISequence>()
            .Where(parentSequence => parentSequence is { IsPending: true, IsCompleting: false })
            .Distinct()
            .ParallelForEachAsync(async parentSequence => await parentSequence.AbortAsync(abortReason).ConfigureAwait(false));

    public static ValueTask AwaitAllProcessingAsync(this IEnumerable<ISequence> sequences) =>
        sequences.ParallelForEachAsync(async sequence => await sequence.AwaitProcessingAsync(false).ConfigureAwait(false));
}
