// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Util
{
    /// <summary>
    ///     This will become obsolete with .NET 5, where the <see cref="ValueTask" /> will expose these static
    ///     methods.
    /// </summary>
    internal static class ValueTaskFactory
    {
        /// <summary>
        ///     Gets a task that has already completed successfully.
        /// </summary>
        public static ValueTask CompletedTask => default;

        /// <summary>
        ///     Creates a <see cref="ValueTask{TResult}" /> that's completed successfully with the specified
        ///     result.
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the result returned by the task.
        /// </typeparam>
        /// <param name="result">
        ///     The result to store into the completed task.
        /// </param>
        /// <returns>
        ///     The successfully completed task.
        /// </returns>
        public static ValueTask<TResult> FromResult<TResult>(TResult result) =>
            new(result);

        /// <summary>
        ///     Creates a <see cref="ValueTask" /> that has completed due to cancellation with the specified
        ///     cancellation token.
        /// </summary>
        /// <param name="cancellationToken">
        ///     The cancellation token with which to complete the task.
        /// </param>
        /// <returns>
        ///     The canceled task.
        /// </returns>
        public static ValueTask FromCanceled(CancellationToken cancellationToken) =>
            new(Task.FromCanceled(cancellationToken));

        /// <summary>
        ///     Creates a <see cref="ValueTask{TResult}" /> that has completed due to cancellation with the specified
        ///     cancellation token.
        /// </summary>
        /// <param name="cancellationToken">
        ///     The cancellation token with which to complete the task.
        /// </param>
        /// <returns>
        ///     The canceled task.
        /// </returns>
        public static ValueTask<TResult> FromCanceled<TResult>(CancellationToken cancellationToken) =>
            new(Task.FromCanceled<TResult>(cancellationToken));

        /// <summary>
        ///     Creates a <see cref="ValueTask" /> that has completed with the specified exception.
        /// </summary>
        /// <param name="exception">
        ///     The exception with which to complete the task.
        /// </param>
        /// <returns>
        ///     The faulted task.
        /// </returns>
        public static ValueTask FromException(Exception exception) =>
            new(Task.FromException(exception));

        /// <summary>
        ///     Creates a <see cref="ValueTask{TResult}" /> that has completed with the specified exception.
        /// </summary>
        /// <param name="exception">
        ///     The exception with which to complete the task.
        /// </param>
        /// <returns>
        ///     The faulted task.
        /// </returns>
        public static ValueTask<TResult> FromException<TResult>(Exception exception) =>
            new(Task.FromException<TResult>(exception));
    }
}
