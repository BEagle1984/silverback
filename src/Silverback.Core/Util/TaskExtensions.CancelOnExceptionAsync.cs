// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static partial class TaskExtensions
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
    public static async Task CancelOnExceptionAsync(this Task task, CancellationTokenSource cancellationTokenSource)
    {
        Task completedTask = await Task.WhenAny(task, cancellationTokenSource.Token.AsTask()).ConfigureAwait(false);

        if (completedTask == task && task.IsFaulted)
        {
            try
            {
#if NETSTANDARD
                cancellationTokenSource.Cancel();
#else
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
#endif
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
    public static async Task<T> CancelOnExceptionAsync<T>(this Task<T> task, CancellationTokenSource cancellationTokenSource)
    {
        Task completedTask = await Task.WhenAny(task, cancellationTokenSource.Token.AsTask()).ConfigureAwait(false);

        if (completedTask == task && task.IsFaulted)
        {
            try
            {
#if NETSTANDARD
                cancellationTokenSource.Cancel();
#else
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
#endif
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }

        return await task.ConfigureAwait(false);
    }
}
