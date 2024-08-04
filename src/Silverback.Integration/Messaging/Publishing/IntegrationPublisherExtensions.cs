// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Adds the <c>WrapAndPublish</c>,<c>WrapAndPublishBatch</c>, <c>WrapAndPublishAsync</c>, and <c>WrapAndPublishBatchAsync</c> methods
///     to the <see cref="IPublisher" /> interface.
/// </summary>
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishAsync<TMessage>(
        this IPublisher publisher,
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = true)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceAsync(message, publisher, producers, envelopeConfigurationAction);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishAsync<TMessage, TArgument>(
        this IPublisher publisher,
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = true)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceAsync(message, publisher, producers, envelopeConfigurationAction, argument);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher, producers, envelopeConfigurationAction);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher, producers, envelopeConfigurationAction, argument);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(sources, nameof(sources));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
        {
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(
                sources,
                publisher,
                producers,
                mapperFunction,
                envelopeConfigurationAction);
        }

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    ///     The argument to be passed to the <paramref name="mapperFunction"/> and the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(sources, nameof(sources));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
        {
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(
                sources,
                publisher,
                producers,
                mapperFunction,
                envelopeConfigurationAction,
                argument);
        }

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher, producers, envelopeConfigurationAction);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher, producers, envelopeConfigurationAction, argument);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(sources, nameof(sources));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
        {
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(
                sources,
                publisher,
                producers,
                mapperFunction,
                envelopeConfigurationAction);
        }

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    ///     The argument to be passed to the <paramref name="mapperFunction"/> and the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(sources, nameof(sources));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
        {
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(
                sources,
                publisher,
                producers,
                mapperFunction,
                envelopeConfigurationAction,
                argument);
        }

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher, producers, envelopeConfigurationAction);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher, producers, envelopeConfigurationAction, argument);

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelopes.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(sources, nameof(sources));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
        {
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(
                sources,
                publisher,
                producers,
                mapperFunction,
                envelopeConfigurationAction);
        }

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
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
    ///     The argument to be passed to the <paramref name="mapperFunction"/> and the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        this IPublisher publisher,
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(sources, nameof(sources));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count != 0)
        {
            return MessageWrapper.Instance.WrapAndProduceBatchAsync(
                sources,
                publisher,
                producers,
                mapperFunction,
                envelopeConfigurationAction,
                argument);
        }

        if (throwIfUnhandled)
            throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

        return Task.CompletedTask;
    }

    private static IProducerCollection GetProducerCollection(SilverbackContext context) =>
        context.GetOrAddObject(
            ProducerCollectionObjectTypeId,
            static serviceProvider => serviceProvider.GetRequiredService<IProducerCollection>(),
            context.ServiceProvider);
}
