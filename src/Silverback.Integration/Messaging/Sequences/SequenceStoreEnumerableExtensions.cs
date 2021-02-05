// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    internal static class SequenceStoreEnumerableExtensions
    {
        public static Task DisposeAllAsync(
            this IEnumerable<ISequenceStore> stores,
            SequenceAbortReason abortReason) =>
            stores
                .SelectMany(store => store)
                .ToList()
                .ParallelForEachAsync(
                    async sequence =>
                    {
                        if (sequence.IsPending)
                        {
                            await sequence.AbortAsync(abortReason)
                                .ConfigureAwait(false);
                        }

                        await sequence.AwaitProcessingAsync(false).ConfigureAwait(false);
                    });
    }
}
