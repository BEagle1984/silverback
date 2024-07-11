// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
    ///     The message is wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceAsync<TMessage>(
        TMessage? message,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     The message is wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceAsync<TMessage, TArgument>(
        TMessage? message,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class;

    /// <summary>
    ///     The messages are wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceBatchAsync<TMessage>(
        IReadOnlyCollection<TMessage> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     The messages are wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IReadOnlyCollection<TMessage> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class;

    /// <summary>
    ///     The messages are wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceBatchAsync<TMessage>(
        IEnumerable<TMessage> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     The messages are wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IEnumerable<TMessage> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class;

    /// <summary>
    ///     The messages are wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceBatchAsync<TMessage>(
        IAsyncEnumerable<TMessage> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     The messages are wrapped in an <see cref="IOutboundEnvelope{TMessage}" /> and produced.
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
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="actionArgument">
    ///     The argument to be passed to the <paramref name="envelopeConfigurationAction" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IAsyncEnumerable<TMessage> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class;
}
