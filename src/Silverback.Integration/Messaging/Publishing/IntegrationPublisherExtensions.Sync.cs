// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <content>
///     Adds the <c>WrapAndPublish</c> methods to the <see cref="IPublisher" /> interface.
/// </content>
public static partial class IntegrationPublisherExtensions
{
    /// <summary>
    ///     Wraps the message in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes it.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to be published.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="message">
    ///     The message to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublish<TMessage>(
        this IPublisher publisher,
        TMessage message,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = true)
        where TMessage : class =>
        WrapAndPublishAsync(publisher, message, envelopeConfigurationAction, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the message in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes it.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to be published.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="message">
    ///     The message to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublish<TMessage, TArgument>(
        this IPublisher publisher,
        TMessage message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = true)
        where TMessage : class
        => WrapAndPublishAsync(publisher, message, envelopeConfigurationAction, actionArgument, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messages">
    ///     The messages to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublishBatch<TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
        => WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messages">
    ///     The messages to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublishBatch<TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = false)
        where TMessage : class
        => WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, actionArgument, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messages">
    ///     The messages to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublishBatch<TMessage>(
        this IPublisher publisher,
        IEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
        => WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messages">
    ///     The messages to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublishBatch<TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = false)
        where TMessage : class
        => WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, actionArgument, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messages">
    ///     The messages to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublishBatch<TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
        => WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, throwIfUnhandled).SafeWait();

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messages">
    ///     The messages to be published.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    public static void WrapAndPublishBatch<TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = false)
        where TMessage : class
        => WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, actionArgument, throwIfUnhandled).SafeWait();
}
