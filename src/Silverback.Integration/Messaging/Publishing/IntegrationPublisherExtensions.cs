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
///     Adds the <c>WrapAndPublishAsync</c> methods to the <see cref="IPublisher" /> interface.
/// </summary>
public static class IntegrationPublisherExtensions
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
        TMessage message,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = true)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(message, nameof(message));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceAsync(message, publisher.Context, producers, envelopeConfigurationAction);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
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
        TMessage message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = true)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(message, nameof(message));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceAsync(message, publisher.Context, producers, envelopeConfigurationAction, actionArgument);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IReadOnlyCollection<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.HasNoNulls(messages, nameof(messages));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher.Context, producers, envelopeConfigurationAction);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
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
        IReadOnlyCollection<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.HasNoNulls(messages, nameof(messages));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher.Context, producers, envelopeConfigurationAction, actionArgument);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher.Context, producers, envelopeConfigurationAction);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
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
        IEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher.Context, producers, envelopeConfigurationAction, actionArgument);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A value indicating whether an exception should be thrown if no producer is found for the message type.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task WrapAndPublishBatchAsync<TMessage>(
        this IPublisher publisher,
        IAsyncEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher.Context, producers, envelopeConfigurationAction);
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
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
        IAsyncEnumerable<TMessage> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument,
        bool throwIfUnhandled = false)
        where TMessage : class
    {
        Check.NotNull(publisher, nameof(publisher));
        Check.NotNull(messages, nameof(messages));
        Check.NotNull(envelopeConfigurationAction, nameof(envelopeConfigurationAction));

        IReadOnlyCollection<IProducer> producers = GetProducerCollection(publisher.Context).GetProducersForMessage(typeof(TMessage));

        if (producers.Count == 0)
        {
            if (throwIfUnhandled)
                throw new RoutingException($"No producer found for message of type '{typeof(TMessage).Name}'.");

            return Task.CompletedTask;
        }

        return MessageWrapper.Instance.WrapAndProduceBatchAsync(messages, publisher.Context, producers, envelopeConfigurationAction, actionArgument);
    }

    private static IProducerCollection GetProducerCollection(SilverbackContext context) =>
        context.GetOrAddObject(
            ProducerCollectionObjectTypeId,
            static serviceProvider => serviceProvider.GetRequiredService<IProducerCollection>(),
            context.ServiceProvider);
}
