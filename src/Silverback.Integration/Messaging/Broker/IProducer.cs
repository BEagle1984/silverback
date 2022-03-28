// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
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
    ///     Gets the related <see cref="IBrokerClient" />.
    /// </summary>
    IBrokerClient Client { get; }

    /// <summary>
    ///     Gets the endpoint configuration.
    /// </summary>
    ProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be delivered.
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
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void Produce(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the message to be delivered.
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
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    IBrokerMessageIdentifier? RawProduce(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null);

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
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void RawProduce(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

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
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void RawProduce(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="messageContent">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void RawProduce(
        ProducerEndpoint endpoint,
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     In this implementation the message is synchronously enqueued but produced asynchronously. The callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="messageStream">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    void RawProduce(
        ProducerEndpoint endpoint,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message to be delivered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="ValueTask" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The <see cref="ValueTask" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    ValueTask ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="ValueTask" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the message to be delivered.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The <see cref="ValueTask" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    ValueTask ProduceAsync(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        ProducerEndpoint endpoint,
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
    /// </returns>
    ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        ProducerEndpoint endpoint,
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers = null);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="ValueTask" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The <see cref="ValueTask" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    ValueTask RawProduceAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="ValueTask" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The <see cref="ValueTask" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    ValueTask RawProduceAsync(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="ValueTask" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The <see cref="ValueTask" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    ValueTask RawProduceAsync(
        ProducerEndpoint endpoint,
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);

    /// <summary>
    ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="ValueTask" /> completes when the message is enqueued while the callbacks
    ///     are called when the message is actually produced (or the produce failed).
    /// </remarks>
    /// <param name="endpoint">
    ///     The target endpoint.
    /// </param>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="headers">
    ///     The optional message headers.
    /// </param>
    /// <param name="onSuccess">
    ///     The callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     The callback to be invoked when the produce fails.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The <see cref="ValueTask" /> will complete as
    ///     soon as the message is enqueued.
    /// </returns>
    ValueTask RawProduceAsync(
        ProducerEndpoint endpoint,
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);
}
