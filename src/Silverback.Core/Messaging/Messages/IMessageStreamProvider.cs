// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}"/>.
    /// </summary>
    internal interface IMessageStreamProvider
    {
        /// <summary>
        ///     Gets the type of the messages being streamed.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of that will be linked with this
        ///     provider and will be pushed with the messages matching the type <paramref name="messageType"/>.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages being streamed to the linked stream.
        /// </param>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        IMessageStreamEnumerable<object> CreateStream(Type messageType);

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> of that will be linked with this
        ///     provider and will be pushed with the messages matching the type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages being streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        IMessageStreamEnumerable<TMessage> CreateStream<TMessage>();

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
