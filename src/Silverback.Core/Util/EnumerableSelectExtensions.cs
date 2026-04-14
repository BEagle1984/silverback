// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableSelectExtensions
{
    public static IEnumerable<TResult> ParallelSelect<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector,
        int? maxDegreeOfParallelism = null)
    {
        ConcurrentBag<TResult> values = [];
        Parallel.ForEach(
            source,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1 },
            value => values.Add(selector(value)));
        return values;
    }

    public static ValueTask<IReadOnlyCollection<TResult>> ParallelSelectAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<TResult>> selector) =>
        source.ParallelSelect(selector).AwaitAllAsync();

    public static async ValueTask<IEnumerable<TResult>> SelectAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<TResult>> selector)
    {
        List<TResult> results = [];

        async ValueTask InvokeSelectorAsync(T item) => results.Add(await selector.Invoke(item).ConfigureAwait(false));

        await source.ForEachAsync(InvokeSelectorAsync).ConfigureAwait(false);
        return results;
    }
}
