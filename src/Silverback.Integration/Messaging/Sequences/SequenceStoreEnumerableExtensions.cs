// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Messaging.Sequences
{
    internal static class SequenceStoreEnumerableExtensions
    {
        public static Task AbortAllSequencesAsync(
            this IEnumerable<ISequenceStore> stores,
            SequenceAbortReason abortReason) =>
            stores
                .SelectMany(store => store)
                .ToList()
                .AbortAllAsync(abortReason);
    }
}
