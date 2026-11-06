// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableForEachExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T element in source)
        {
            action(element);
        }
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        int index = 0;
        foreach (T element in source)
        {
            action(element, index++);
        }
    }

    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        foreach (T element in source)
        {
            await action(element).ConfigureAwait(false);
        }
    }

    public static async ValueTask ForEachAsync<T>(this IEnumerable<T> source, Func<T, ValueTask> action)
    {
        foreach (T element in source)
        {
            await action(element).ConfigureAwait(false);
        }
    }

    public static ValueTask ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, ValueTask> action) =>
        source
            .Select(item => new ValueTask(Task.Run(() => action(item).AsTask())))
            .AwaitAllAsync();
}
