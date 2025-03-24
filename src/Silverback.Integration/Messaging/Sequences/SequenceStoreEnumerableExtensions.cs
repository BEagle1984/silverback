// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Messaging.Sequences;

internal static class SequenceStoreEnumerableExtensions
{
    public static ValueTask AbortAllSequencesAsync(this IEnumerable<ISequenceStore> stores, SequenceAbortReason abortReason) =>
        stores.SelectMany(store => store).ToList().AbortAllAsync(abortReason);
}
