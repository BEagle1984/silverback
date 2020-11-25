// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class TaskExtensions
    {
        /// <summary>
        ///     Awaits the specified <see cref="Task" /> and returns a <see cref="Task{T}" /> that wrap either the
        ///     task result or null.
        /// </summary>
        /// <param name="task">
        ///     The <see cref="Task" /> to be awaited.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains
        ///     either the result of the awaited task or <c>null</c>.
        /// </returns>
        public static async Task<object?> GetReturnValueAsync(this Task task)
        {
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");

            return resultProperty?.GetValue(task);
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
        public static async Task CancelOnExceptionAsync(this Task task, CancellationTokenSource cancellationTokenSource)
        {
            // TODO: Array pool?
            var tasks = new[] { task, cancellationTokenSource.Token.AsTask() };
            await Task.WhenAny(tasks).ConfigureAwait(false);

            var exception = tasks.Where(t => t.IsFaulted).Select(t => t.Exception).FirstOrDefault();

            if (exception != null)
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
            // TODO: Array pool?
            var tasks = new[] { task, cancellationTokenSource.Token.AsTask() };
            await Task.WhenAny(tasks).ConfigureAwait(false);

            var exception = tasks.Where(t => t.IsFaulted).Select(t => t.Exception).FirstOrDefault();

            if (exception != null)
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
    }
}
