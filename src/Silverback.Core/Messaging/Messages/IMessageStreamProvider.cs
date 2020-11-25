// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}" />.
    /// </summary>
    public interface IMessageStreamProvider
    {
        /// <summary>
        ///     Gets the type of the messages being streamed.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Gets a value indicating whether the stream can be forwarded to the subscribers declare a parameter of
        ///     type <see cref="IEnumerable{T}" />.
        /// </summary>
        bool AllowSubscribeAsEnumerable { get; }

        /// <summary>
        ///     Gets the number of <see cref="IMessageStreamEnumerable{TMessage}" /> that have been created via
        ///     <see cref="CreateStream" /> or <see cref="CreateStream{TMessage}" />.
        /// </summary>
        int StreamsCount { get; }

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> that will be linked with this
        ///     provider and will be pushed with the messages matching the type <paramref name="messageType" />.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be streamed to the linked stream.
        /// </param>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        IMessageStreamEnumerable<object> CreateStream(Type messageType);

        /// <summary>
        ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> that will be linked with this
        ///     provider and will be pushed with the messages matching the type <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        IMessageStreamEnumerable<TMessage> CreateStream<TMessage>();

        /// <summary>
        ///     Creates a new <see cref="ILazyMessageStreamEnumerable{TMessage}" /> that will be linked with this
        ///     provider and will create the <see cref="IMessageStreamEnumerable{TMessage}" /> as soon as a message
        ///     matching the type <paramref name="messageType" /> is pushed.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be streamed to the linked stream.
        /// </param>
        /// <returns>
        ///     The linked <see cref="ILazyMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        ILazyMessageStreamEnumerable<object> CreateLazyStream(Type messageType);

        /// <summary>
        ///     Creates a new <see cref="ILazyMessageStreamEnumerable{TMessage}" /> that will be linked with this
        ///     provider and will create the <see cref="IMessageStreamEnumerable{TMessage}" /> as soon as a message
        ///     matching the type <typeparamref name="TMessage" /> is pushed.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be streamed to the linked stream.
        /// </typeparam>
        /// <returns>
        ///     The linked <see cref="ILazyMessageStreamEnumerable{TMessage}" />.
        /// </returns>
        ILazyMessageStreamEnumerable<TMessage> CreateLazyStream<TMessage>();
    }
}
