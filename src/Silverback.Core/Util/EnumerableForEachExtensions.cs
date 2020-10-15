// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Util
{
    // TODO: Test
    internal static class EnumerableForEachExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            foreach (var element in source)
            {
                await action(element).ConfigureAwait(false);
            }
        }

        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> source, Action<T> action)
        {
            await foreach (T element in source)
            {
                action(element);
            }
        }

        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> action)
        {
            await foreach (var element in source)
            {
                await action(element).ConfigureAwait(false);
            }
        }

        public static void ParallelForEach<T>(
            this IEnumerable<T> source,
            Action<T> action,
            int? maxDegreeOfParallelism = null) =>
            Parallel.ForEach(
                source,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1 },
                action);

        // http://blog.briandrupieski.com/throttling-asynchronous-methods-in-csharp
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task> action,
            int? maxDegreeOfParallelism = null) =>
            source.ParallelSelectAsync(
                async s =>
                {
                    await action(s).ConfigureAwait(false);
                    return 0;
                },
                maxDegreeOfParallelism);
    }
}
