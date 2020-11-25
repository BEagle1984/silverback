// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class EnumerableFirstExtensions
    {
        public static async Task<T> FirstOrDefaultAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task<bool>> predicate)
        {
            foreach (var item in source)
            {
                if (await predicate(item).ConfigureAwait(false))
                    return item;
            }

            // Should probably use [result:MaybeNull] to suppress the warning but it doesn't work as expected in the
            // caller side
            return default!;
        }
    }
}
