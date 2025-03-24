// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static partial class TaskExtensions
{
    public static ValueTask AwaitAllAsync(this IEnumerable<ValueTask> tasks) => AwaitAllAsync(tasks.ToList());

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rethrown")]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive because in a loop")]
    public static async ValueTask AwaitAllAsync(this IReadOnlyCollection<ValueTask> tasks)
    {
        Check.NotNull(tasks, nameof(tasks));

        List<Exception>? exceptions = null;

        foreach (ValueTask task in tasks)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>(tasks.Count);
                exceptions.Add(ex);
            }
        }

        if (exceptions != null)
            throw new AggregateException(exceptions);
    }

    public static ValueTask<IReadOnlyCollection<TResult>> AwaitAllAsync<TResult>(this IEnumerable<ValueTask<TResult>> tasks) =>
        AwaitAllAsync(tasks.ToList());

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rethrown")]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive because in a loop")]
    public static async ValueTask<IReadOnlyCollection<TResult>> AwaitAllAsync<TResult>(this IReadOnlyCollection<ValueTask<TResult>> tasks)
    {
        Check.NotNull(tasks, nameof(tasks));

        List<Exception>? exceptions = null;
        List<TResult> results = new(tasks.Count);

        foreach (ValueTask<TResult> task in tasks)
        {
            try
            {
                results.Add(await task.ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>(tasks.Count);
                exceptions.Add(ex);
            }
        }

        if (exceptions != null)
            throw new AggregateException(exceptions);

        return results;
    }
}
