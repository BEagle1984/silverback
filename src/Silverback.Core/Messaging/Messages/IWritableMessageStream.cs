// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Declares the PushAsync method, used to push to the linked streams.
    /// </summary>
    internal interface IWritableMessageStream
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
        ValueTask PushAsync(object message, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed. This will also end the
        ///     enumerator loop.
        /// </summary>
        void Complete();

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
    }
}
