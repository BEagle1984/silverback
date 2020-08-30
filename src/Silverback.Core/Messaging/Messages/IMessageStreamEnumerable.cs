// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a stream of messages being published through the internal bus. It is an enumerable that is
    ///     asynchronously pushed with messages.
    /// </summary>
    internal interface IMessageStreamEnumerable
    {
        /// <summary>
        ///     Gets the type of the messages being streamed.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Adds the specified message to the stream. The returned <see cref="Task" /> will complete only when the
        ///     message has actually been pulled and processed.
        /// </summary>
        /// <param name="pushedMessage">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The  <see cref="Task" /> will complete
        ///     only when the message has actually been pulled and processed.
        /// </returns>
        Task PushAsync(PushedMessage pushedMessage, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Aborts the ongoing enumeration and the pending calls to <see cref="PushAsync" />, then marks the
        ///     stream as complete. Calling this method will cause an <see cref="OperationCanceledException" /> to be
        ///     thrown by the enumerator and the <see cref="PushAsync" /> method.
        /// </summary>
        void Abort();

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CompleteAsync(CancellationToken cancellationToken = default);
    }
}
