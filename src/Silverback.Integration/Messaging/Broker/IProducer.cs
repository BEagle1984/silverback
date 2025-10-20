// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Produces the messages to an endpoint.
/// </summary>
public interface IProducer
{
    /// <summary>
    ///     Gets the producer name.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     Gets the endpoint configuration.
    /// </summary>
    ProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <summary>
    ///     Gets the <see cref="IOutboundEnvelopeFactory" /> used to create the envelopes wrapping the messages to be produced.
    /// </summary>
    IOutboundEnvelopeFactory EnvelopeFactory { get; }

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to be produced.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? Produce<TMessage>(TMessage? message, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <typeparam name="TMessage">
    ///     The type of the message to be produced.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    void Produce<TMessage>(
        TMessage? message,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <typeparam name="TMessage">
    ///     The type of the message to be produced.
    /// </typeparam>
    /// <typeparam name="TState">
    ///     The type of the state object to be passed to the callbacks.
    /// </typeparam>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="state">
    ///     The state object to be passed to the callbacks.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    void Produce<TMessage, TState>(
        TMessage? message,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class;

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void Produce(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <typeparam name="TState">
    ///     The type of the state object to be passed to the callbacks.
    /// </typeparam>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="state">
    ///     The state object to be passed to the callbacks.
    /// </param>
    void Produce<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(IOutboundEnvelope envelope);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    void RawProduce(
        byte[]? messageContent,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <typeparam name="TState">
    ///     The type of the state object to be passed to the callbacks.
    /// </typeparam>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="state">
    ///     The state object to be passed to the callbacks.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    void RawProduce<TState>(
        byte[]? messageContent,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    void RawProduce(
        Stream? messageStream,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <typeparam name="TState">
    ///     The type of the state object to be passed to the callbacks.
    /// </typeparam>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="state">
    ///     The state object to be passed to the callbacks.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    void RawProduce<TState>(
        Stream? messageStream,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void RawProduce(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <typeparam name="TState">
    ///     The type of the state object to be passed to the callbacks.
    /// </typeparam>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <param name="state">
    ///     The state object to be passed to the callbacks.
    /// </param>
    void RawProduce<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> ProduceAsync(
        object? message,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        byte[]? messageContent,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="envelopeConfigurationAction">
    ///     An optional action that can be used to configure the envelope.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        Stream? messageStream,
        Action<IOutboundEnvelope>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be produced.
    /// </param>     /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        IOutboundEnvelope envelope,
        CancellationToken cancellationToken = default);
}
