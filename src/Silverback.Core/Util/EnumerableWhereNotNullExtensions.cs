// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Util
{
    internal static class EnumerableWhereNotNullExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
            where T : class
        {
            Check.NotNull(enumerable, nameof(enumerable));

            foreach (var item in enumerable)
            {
                if (item != null)
                    yield return item;
            }
        }
    }
}
