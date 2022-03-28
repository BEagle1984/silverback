// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class EnumerableSelectExtensions
{
    public static IEnumerable<TResult> ParallelSelect<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector,
        int? maxDegreeOfParallelism = null)
    {
        ConcurrentBag<TResult> values = new();
        Parallel.ForEach(
            source,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1 },
            value => values.Add(selector(value)));
        return values;
    }

    // http://blog.briandrupieski.com/throttling-asynchronous-methods-in-csharp
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "ValueTask is awaited")]
    public static async ValueTask<IEnumerable<TResult>> ParallelSelectAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<TResult>> selector,
        int? maxDegreeOfParallelism = null)
    {
        if (maxDegreeOfParallelism == null)
            return await source.ParallelSelect(selector).AwaitAllAsync().ConfigureAwait(false);

        if (maxDegreeOfParallelism == 1)
            return await source.SelectAsync(selector).ConfigureAwait(false);

        using SemaphoreSlim semaphore = new(maxDegreeOfParallelism.Value);
        using CancellationTokenSource cancellationTokenSource = new();

        async ValueTask<TResult> InvokeSelector(T value)
        {
            await semaphore.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                TResult result = await selector(value).ConfigureAwait(false);
                semaphore.Release();
                return result;
            }
            catch (Exception)
            {
                cancellationTokenSource.Cancel();
                throw;
            }
        }

        return await source.ParallelSelect(InvokeSelector).AwaitAllAsync().ConfigureAwait(false);
    }

    public static IEnumerable<TResult> Select<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector,
        bool parallel,
        int? maxDegreeOfParallelism = null)
    {
        return parallel
            ? source.ParallelSelect(selector, maxDegreeOfParallelism)
            : source.Select(selector);
    }

    public static async ValueTask<IEnumerable<TResult>> SelectAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<TResult>> selector)
    {
        List<TResult> results = new();

        async ValueTask InvokeSelector(T item)
        {
            results.Add(await selector.Invoke(item).ConfigureAwait(false));
        }

        await source.ForEachAsync(InvokeSelector).ConfigureAwait(false);
        return results;
    }

    public static ValueTask<IEnumerable<TResult>> SelectAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<TResult>> selector,
        bool parallel,
        int? maxDegreeOfParallelism = null)
    {
        return parallel
            ? source.ParallelSelectAsync(selector, maxDegreeOfParallelism)
            : source.SelectAsync(selector);
    }

    public static async ValueTask<IEnumerable<TResult>> SelectManyAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<IEnumerable<TResult>>> selector)
    {
        IEnumerable<IEnumerable<TResult>> results = await SelectAsync(source, selector).ConfigureAwait(false);
        return results.SelectMany(result => result);
    }

    public static async ValueTask<IEnumerable<TResult>> ParallelSelectManyAsync<T, TResult>(
        this IEnumerable<T> source,
        Func<T, ValueTask<IEnumerable<TResult>>> selector,
        int? maxDegreeOfParallelism = null)
    {
        IEnumerable<IEnumerable<TResult>> results = await ParallelSelectAsync(source, selector, maxDegreeOfParallelism).ConfigureAwait(false);
        return results.SelectMany(result => result);
    }
}
