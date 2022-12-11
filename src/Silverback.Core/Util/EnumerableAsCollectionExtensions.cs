// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Util;

internal static class EnumerableAsCollectionExtensions
{
    public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is IReadOnlyCollection<T> collection)
            return collection;

        return enumerable.ToList();
    }

    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is IReadOnlyList<T> collection)
            return collection;

        return enumerable.ToList();
    }

    public static List<T> AsList<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is List<T> list)
            return list;

        return enumerable.ToList();
    }

    public static T[] AsArray<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable is T[] array)
            return array;

        return enumerable.ToArray();
    }
}
