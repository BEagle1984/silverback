// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Messages;

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
    /// <param name="filters">
    ///     The filters to be applied.
    /// </param>
    /// <returns>
    ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
    /// </returns>
    [SuppressMessage("", "VSTHRD200", Justification = "IMessageStreamEnumerable is both sync and async")]
    IMessageStreamEnumerable<object> CreateStream(
        Type messageType,
        IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <summary>
    ///     Creates a new <see cref="IMessageStreamEnumerable{TMessage}" /> that will be linked with this
    ///     provider and will be pushed with the messages matching the type <typeparamref name="TMessage" />.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be streamed to the linked stream.
    /// </typeparam>
    /// <param name="filters">
    ///     The filters to be applied.
    /// </param>
    /// <returns>
    ///     The linked <see cref="IMessageStreamEnumerable{TMessage}" />.
    /// </returns>
    [SuppressMessage("", "VSTHRD200", Justification = "IMessageStreamEnumerable is both sync and async")]
    IMessageStreamEnumerable<TMessage> CreateStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <summary>
    ///     Creates a new <see cref="ILazyMessageStreamEnumerable{TMessage}" /> that will be linked with this
    ///     provider and will create the <see cref="IMessageStreamEnumerable{TMessage}" /> as soon as a message
    ///     matching the type <paramref name="messageType" /> is pushed.
    /// </summary>
    /// <param name="messageType">
    ///     The type of the messages to be streamed to the linked stream.
    /// </param>
    /// <param name="filters">
    ///     The filters to be applied.
    /// </param>
    /// <returns>
    ///     The linked <see cref="ILazyMessageStreamEnumerable{TMessage}" />.
    /// </returns>
    ILazyMessageStreamEnumerable<object> CreateLazyStream(
        Type messageType,
        IReadOnlyCollection<IMessageFilter>? filters = null);

    /// <summary>
    ///     Creates a new <see cref="ILazyMessageStreamEnumerable{TMessage}" /> that will be linked with this
    ///     provider and will create the <see cref="IMessageStreamEnumerable{TMessage}" /> as soon as a message
    ///     matching the type <typeparamref name="TMessage" /> is pushed.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be streamed to the linked stream.
    /// </typeparam>
    /// <param name="filters">
    ///     The filters to be applied.
    /// </param>
    /// <returns>
    ///     The linked <see cref="ILazyMessageStreamEnumerable{TMessage}" />.
    /// </returns>
    ILazyMessageStreamEnumerable<TMessage> CreateLazyStream<TMessage>(IReadOnlyCollection<IMessageFilter>? filters = null);
}
