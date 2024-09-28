// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and produces them using the specified producers.
/// </summary>
public interface IMessageWrapper
{
    /// <summary>
    ///     Wraps the message in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes it.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to be produced.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceAsync<TMessage>(
        TMessage? message,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the message in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes it.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceAsync<TMessage, TArgument>(
        TMessage? message,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="messages">
    ///     The messages to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TMessage>(
        IReadOnlyCollection<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="messages">
    ///     The messages to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IReadOnlyCollection<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TSource, TMessage>(
        IReadOnlyCollection<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

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
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TSource, TMessage, TArgument>(
        IReadOnlyCollection<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="messages">
    ///     The messages to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TMessage>(
        IEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="messages">
    ///     The messages to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TSource, TMessage>(
        IEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

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
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TSource, TMessage, TArgument>(
        IEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="messages">
    ///     The messages to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TMessage>(
        IAsyncEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Wraps the messages in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <typeparam name="TArgument">
    ///     The type of the argument passed to the <paramref name="envelopeConfigurationAction" />.
    /// </typeparam>
    /// <param name="messages">
    ///     The messages to be produced.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IAsyncEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    ///     Maps the source objects into messages, wraps them in an <see cref="IOutboundEnvelope{TMessage}" /> and publishes them.
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of the source objects.
    /// </typeparam>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be produced.
    /// </typeparam>
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TSource, TMessage>(
        IAsyncEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

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
    /// <param name="sources">
    ///     The source objects to be mapped.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="producers">
    ///     The producers to be used to produce the message.
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
    Task WrapAndProduceBatchAsync<TSource, TMessage, TArgument>(
        IAsyncEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
