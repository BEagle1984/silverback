// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util
{
    // TODO: Test
    internal static class EnumerableSelectExtensions
    {
        public static IEnumerable<TResult> ParallelSelect<T, TResult>(
            this IEnumerable<T> source,
            Func<T, TResult> selector,
            int? maxDegreeOfParallelism = null)
        {
            var values = new ConcurrentBag<TResult>();
            Parallel.ForEach(
                source,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1 },
                value => values.Add(selector(value)));
            return values;
        }

        // http://blog.briandrupieski.com/throttling-asynchronous-methods-in-csharp
        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Task is awaited")]
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        public static async Task<IEnumerable<TResult>> ParallelSelectAsync<T, TResult>(
            this IEnumerable<T> source,
            Func<T, Task<TResult>> selector,
            int? maxDegreeOfParallelism = null)
        {
            if (maxDegreeOfParallelism == null)
                return await Task.WhenAll(source.ParallelSelect(selector)).ConfigureAwait(false);

            if (maxDegreeOfParallelism == 1)
                return await source.SelectAsync(selector).ConfigureAwait(false);

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism.Value);

            var tasks = source.ParallelSelect(
                async value =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        return await selector(value).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

            return await Task.WhenAll(tasks).ConfigureAwait(false);
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

        public static async Task<IEnumerable<TResult>> SelectAsync<T, TResult>(
            this IEnumerable<T> source,
            Func<T, Task<TResult>> selector)
        {
            var results = new List<TResult>();
            await source.ForEachAsync(async s => results.Add(await selector(s).ConfigureAwait(false)))
                .ConfigureAwait(false);
            return results;
        }

        public static Task<IEnumerable<TResult>> SelectAsync<T, TResult>(
            this IEnumerable<T> source,
            Func<T, Task<TResult>> selector,
            bool parallel,
            int? maxDegreeOfParallelism = null)
        {
            return parallel
                ? source.ParallelSelectAsync(selector, maxDegreeOfParallelism)
                : source.SelectAsync(selector);
        }

        public static async Task<IEnumerable<TResult>> SelectManyAsync<T, TResult>(
            this IEnumerable<T> source,
            Func<T, Task<IEnumerable<TResult>>> selector)
        {
            var results = await SelectAsync(source, selector).ConfigureAwait(false);
            return results.SelectMany(r => r);
        }

        public static async Task<IEnumerable<TResult>> ParallelSelectManyAsync<T, TResult>(
            this IEnumerable<T> source,
            Func<T, Task<IEnumerable<TResult>>> selector,
            int? maxDegreeOfParallelism = null)
        {
            var results = await ParallelSelectAsync(source, selector, maxDegreeOfParallelism).ConfigureAwait(false);
            return results.SelectMany(r => r);
        }

        public static async Task<IEnumerable<T>> WhereAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task<bool>> predicate)
        {
            var results = new List<T>();
            await source.ForEachAsync(
                async s =>
                {
                    if (await predicate(s).ConfigureAwait(false))
                        results.Add(s);
                }).ConfigureAwait(false);
            return results;
        }
    }
}
