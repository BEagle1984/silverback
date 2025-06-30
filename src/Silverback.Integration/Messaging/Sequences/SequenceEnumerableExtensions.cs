// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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

    public static ValueTask AwaitAllProcessingAsync(this IEnumerable<ISequence> sequences) =>
        sequences.ParallelForEachAsync(async sequence => await sequence.AwaitProcessingAsync(false).ConfigureAwait(false));
}
