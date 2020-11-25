// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class EnumerableWhereExtensions
    {
        public static async Task<IEnumerable<T>> WhereAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task<bool>> predicate)
        {
            var results = new List<T>();

            foreach (var item in source)
            {
                if (await predicate(item).ConfigureAwait(false))
                    results.Add(item);
            }

            return results;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
            where T : class
        {
            Check.NotNull(source, nameof(source));

            foreach (var item in source)
            {
                if (item != null)
                    yield return item;
            }
        }
    }
}
