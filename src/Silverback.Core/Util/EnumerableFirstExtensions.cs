// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableFirstExtensions
{
    public static async ValueTask<T?> FirstOrDefaultAsync<T>(
        this IEnumerable<T> source,
        Func<T, ValueTask<bool>> predicate)
    {
        foreach (T item in source)
        {
            if (await predicate(item).ConfigureAwait(false))
                return item;
        }

        return default;
    }
}
