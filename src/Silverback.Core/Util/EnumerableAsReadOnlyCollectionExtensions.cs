// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util
{
    internal static class EnumerableAsReadOnlyCollectionExtensions
    {
        public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> enumerable) =>
            enumerable.AsReadOnlyList();

        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is IReadOnlyList<T> collection)
                return collection;

            return enumerable.ToList();
        }
    }
}
