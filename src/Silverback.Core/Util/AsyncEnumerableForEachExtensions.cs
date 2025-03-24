// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class AsyncEnumerableForEachExtensions
{
    public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> source, Action<T> action)
    {
        await foreach (T element in source)
        {
            action(element);
        }
    }

    public static async ValueTask ForEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> action)
    {
        await foreach (T element in source)
        {
            await action(element).ConfigureAwait(false);
        }
    }

    public static async ValueTask ForEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, ValueTask> action)
    {
        await foreach (T element in source)
        {
            await action(element).ConfigureAwait(false);
        }
    }
}
