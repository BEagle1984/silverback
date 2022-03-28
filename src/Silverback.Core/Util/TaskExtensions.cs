// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class TaskExtensions
{
    /// <summary>
    ///     Cancels the specified <see cref="CancellationTokenSource" /> if an exception is thrown.
    /// </summary>
    /// <param name="task">
    ///     The <see cref="Task" /> to be awaited.
    /// </param>
    /// <param name="cancellationTokenSource">
    ///     The <see cref="CancellationTokenSource" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static async Task CancelOnExceptionAsync(
        this Task task,
        CancellationTokenSource cancellationTokenSource)
    {
        Task completedTask = await Task.WhenAny(task, cancellationTokenSource.Token.AsTask()).ConfigureAwait(false);

        if (completedTask == task && task.IsFaulted)
        {
            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }

        await task.ConfigureAwait(false);
    }

    /// <summary>
    ///     Cancels the specified <see cref="CancellationTokenSource" /> if an exception is thrown.
    /// </summary>
    /// <param name="task">
    ///     The <see cref="Task" /> to be awaited.
    /// </param>
    /// <param name="cancellationTokenSource">
    ///     The <see cref="CancellationTokenSource" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static async Task<T> CancelOnExceptionAsync<T>(
        this Task<T> task,
        CancellationTokenSource cancellationTokenSource)
    {
        Task completedTask = await Task.WhenAny(task, cancellationTokenSource.Token.AsTask()).ConfigureAwait(false);

        if (completedTask == task && task.IsFaulted)
        {
            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }

        return await task.ConfigureAwait(false);
    }

    public static void FireAndForget(this Task task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

    public static void FireAndForget(this ValueTask task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

    public static void FireAndForget<T>(this Task<T> task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

    public static void FireAndForget<T>(this ValueTask<T> task)
    {
        // This method is used just to trick the compiler and avoid CS4014
    }

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
