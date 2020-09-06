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
        ///     Add the specified message to the stream. This overload is used by the owner stream to push to the
        ///     linked streams.
        /// </summary>
        /// <param name="pushedMessage">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the write operation.
        /// </param>
        /// <returns>
        ///     A <see cref="ValueTask" /> representing the asynchronous operation.
        /// </returns>
        Task PushAsync(PushedMessage pushedMessage, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed.
        /// </summary>
        void Complete();
    }
}
