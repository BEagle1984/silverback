// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Validation;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Adds the <c>WrapAndPublish</c>,<c>WrapAndPublishBatch</c>, <c>WrapAndPublishAsync</c>, and <c>WrapAndPublishBatchAsync</c> methods
///     to the <see cref="IPublisher" /> interface.
/// </summary>
/// <remarks>
///     The exceptions documented on <see cref="IProducer" /> (e.g. <see cref="ProduceException" /> and
///     <see cref="MessageValidationException" />) propagate as well.
/// </remarks>
public static partial class IntegrationPublisherExtensions
{
    private static readonly Guid ProducerCollectionObjectTypeId = new("56db3045-37a7-420a-89af-dfe4c5b1740c");

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishAsync<TMessage>(
        this IPublisher publisher,
        TMessage? message,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishAsync(message, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishAsync<TMessage>(
        this IPublisher publisher,
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);

        return MessageWrapper.Instance.WrapAndProduceAsync(
            message,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishAsync<TMessage, TArgument>(
        this IPublisher publisher,
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceAsync(
            message,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishBatchAsync(messages, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(messages);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            messages,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(messages);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            messages,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishBatchAsync(sources, mapperFunction, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(sources);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            sources,
            publisher,
            GetProducers<TMessage>(publisher),
            mapperFunction,
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type.
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(sources);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            sources,
            publisher,
            GetProducers<TMessage>(publisher),
            mapperFunction,
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishBatchAsync(messages, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(messages);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            messages,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(messages);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            messages,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishBatchAsync(sources, mapperFunction, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(sources);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            sources,
            publisher,
            GetProducers<TMessage>(publisher),
            mapperFunction,
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(sources);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            sources,
            publisher,
            GetProducers<TMessage>(publisher),
            mapperFunction,
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishBatchAsync(messages, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(messages);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            messages,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(messages);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            messages,
            publisher,
            GetProducers<TMessage>(publisher),
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        publisher.WrapAndPublishBatchAsync(sources, mapperFunction, null, cancellationToken);

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(sources);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            sources,
            publisher,
            GetProducers<TMessage>(publisher),
            mapperFunction,
            envelopeConfigurationAction,
            cancellationToken);
    }

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
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="RoutingException">
    ///     No producer is configured for the message type, or more than one producer is configured for it (not supported for enumerable
    ///     batches).
    /// </exception>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        Check.NotNull(publisher);
        Check.NotNull(sources);
        Check.NotNull(envelopeConfigurationAction);

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(
            sources,
            publisher,
            GetProducers<TMessage>(publisher),
            mapperFunction,
            envelopeConfigurationAction,
            argument,
            cancellationToken);
    }

    private static IReadOnlyCollection<IProducer> GetProducers<TMessage>(IPublisher publisher)
    {
        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return producers;
    }

    private static IProducerCollection GetProducerCollection(ISilverbackContext context) =>
        context.GetOrAddObject(
            ProducerCollectionObjectTypeId,
            static serviceProvider => serviceProvider.GetRequiredService<IProducerCollection>(),
            context.ServiceProvider);
}
