// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        public static async Task<object?> GetReturnValue(this Task task)
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
        public static async Task CancelOnException(
            this Task task,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore
                }

                throw;
            }
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
        public static async Task<T> CancelOnException<T>(
            this Task<T> task,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore
                }

                throw;
            }
        }
    }
}
