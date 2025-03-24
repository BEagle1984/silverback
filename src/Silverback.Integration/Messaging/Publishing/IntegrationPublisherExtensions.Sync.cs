// Copyright (c) 2025 Sergio Aquilini
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
    public static void WrapAndPublish<TMessage>(
        this IPublisher publisher,
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishAsync(publisher, message, envelopeConfigurationAction).SafeWait();

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
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublish<TMessage, TArgument>(
        this IPublisher publisher,
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishAsync(publisher, message, envelopeConfigurationAction, argument).SafeWait();

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
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    public static void WrapAndPublishBatch<TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction).SafeWait();

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
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublishBatch<TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, argument).SafeWait();

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="mapperFunction">
    ///     The function used to map the source objects to messages.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    public static void WrapAndPublishBatch<TSource, TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, sources, mapperFunction, envelopeConfigurationAction).SafeWait();

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="mapperFunction">
    ///     The function used to map the source objects to messages.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="mapperFunction" /> and the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublishBatch<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, sources, mapperFunction, envelopeConfigurationAction, argument).SafeWait();

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
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    public static void WrapAndPublishBatch<TMessage>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction).SafeWait();

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
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublishBatch<TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, argument).SafeWait();

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="mapperFunction">
    ///     The function used to map the source objects to messages.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    public static void WrapAndPublishBatch<TSource, TMessage>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, sources, mapperFunction, envelopeConfigurationAction).SafeWait();

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="mapperFunction">
    ///     The function used to map the source objects to messages.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="mapperFunction" /> and the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublishBatch<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, sources, mapperFunction, envelopeConfigurationAction, argument).SafeWait();

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
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    public static void WrapAndPublishBatch<TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction).SafeWait();

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
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublishBatch<TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, messages, envelopeConfigurationAction, argument).SafeWait();

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="mapperFunction">
    ///     The function used to map the source objects to messages.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    public static void WrapAndPublishBatch<TSource, TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, sources, mapperFunction, envelopeConfigurationAction).SafeWait();

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="mapperFunction">
    ///     The function used to map the source objects to messages.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     The action used to configure the envelopes.
    /// </param>
    /// <param name="argument">
    ///     The argument to be passed to the <paramref name="mapperFunction" /> and the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    public static void WrapAndPublishBatch<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        WrapAndPublishBatchAsync(publisher, sources, mapperFunction, envelopeConfigurationAction, argument).SafeWait();
}
