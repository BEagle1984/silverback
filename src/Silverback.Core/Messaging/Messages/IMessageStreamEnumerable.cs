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
        ///     Gets the (base) type of the messages being streamed.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Add the specified message to the stream.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the write operation.
        /// </param>
        /// <returns>
        ///     A <see cref="ValueTask" /> representing the asynchronous operation.
        /// </returns>
        Task PushAsync(object message, CancellationToken cancellationToken = default);

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
        ///     Marks the stream as complete, meaning no more messages will be pushed. This will also end the
        ///     enumerator loop.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CompleteAsync();

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of a different message type that is
        ///     linked with this instance and will be pushed with the same messages.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages being streamed to the linked stream.
        /// </param>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        IMessageStreamEnumerable<object> CreateLinkedStream(Type messageType);

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of a different message type that is
        ///     linked with this instance and will be pushed with the same messages.
        /// </summary>
        /// <typeparam name="TMessageLinked">
        ///     The type of the messages being streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        IMessageStreamEnumerable<TMessageLinked> CreateLinkedStream<TMessageLinked>();

        /// <summary>
        ///     Used by the linked stream to notify the main stream that the message has been processed.
        /// </summary>
        /// <param name="pushedMessage">
        ///     The processed message.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task NotifyLinkedStreamProcessed(PushedMessage pushedMessage);

        /// <summary>
        ///     Used by the linked stream to notify the main stream that the enumeration completed.
        /// </summary>
        /// <param name="linkedStream">
        ///     The linked stream.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task NotifyLinkedStreamEnumerationCompleted(IMessageStreamEnumerable linkedStream);
    }
}
