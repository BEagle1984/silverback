// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Produces to an endpoint.
    /// </summary>
    public interface IProducer
    {
        /// <summary>
        ///     Gets the <see cref="InstanceIdentifier" /> uniquely identifying the producer instance.
        /// </summary>
        InstanceIdentifier Id { get; }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this .
        /// </summary>
        IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IProducerEndpoint" /> this instance is connected to.
        /// </summary>
        IProducerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets a value indicating whether this producer is connected to the message broker and ready to produce
        ///     messages.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Gets a value indicating whether this producer is trying to connect to the message broker.
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        ///     Initializes the connection to the message broker (if needed).
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ConnectAsync();

        /// <summary>
        ///     Disconnects from the message broker (if needed).
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task DisconnectAsync();

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="message">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        IBrokerMessageIdentifier? Produce(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null);

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
        ///     The message to be delivered.
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
        ///     The message to be delivered.
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
        ///     The message to be delivered.
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
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageContent">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        IBrokerMessageIdentifier? RawProduce(
            string actualEndpointName,
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageStream">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        IBrokerMessageIdentifier? RawProduce(
            string actualEndpointName,
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
        ///     The message to be delivered.
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
        ///     The message to be delivered.
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
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageContent">
        ///     The message to be delivered.
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
            string actualEndpointName,
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
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageStream">
        ///     The message to be delivered.
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
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="message">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        Task<IBrokerMessageIdentifier?> ProduceAsync(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be delivered.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        Task<IBrokerMessageIdentifier?> ProduceAsync(
            IOutboundEnvelope envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
        ///     are called when the message is actually produced (or the produce failed).
        /// </remarks>
        /// <param name="message">
        ///     The message to be delivered.
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
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
        ///     soon as the message is enqueued.
        /// </returns>
        Task ProduceAsync(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
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
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
        ///     soon as the message is enqueued.
        /// </returns>
        Task ProduceAsync(
            IOutboundEnvelope envelope,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="messageContent">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        Task<IBrokerMessageIdentifier?> RawProduceAsync(
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="messageStream">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        Task<IBrokerMessageIdentifier?> RawProduceAsync(
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageContent">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        Task<IBrokerMessageIdentifier?> RawProduceAsync(
            string actualEndpointName,
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageStream">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="IBrokerMessageIdentifier" /> of the produced record.
        /// </returns>
        Task<IBrokerMessageIdentifier?> RawProduceAsync(
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
        ///     are called when the message is actually produced (or the produce failed).
        /// </remarks>
        /// <param name="messageContent">
        ///     The message to be delivered.
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
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
        ///     soon as the message is enqueued.
        /// </returns>
        Task RawProduceAsync(
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
        ///     are called when the message is actually produced (or the produce failed).
        /// </remarks>
        /// <param name="messageStream">
        ///     The message to be delivered.
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
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
        ///     soon as the message is enqueued.
        /// </returns>
        Task RawProduceAsync(
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
        ///     are called when the message is actually produced (or the produce failed).
        /// </remarks>
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageContent">
        ///     The message to be delivered.
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
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
        ///     soon as the message is enqueued.
        /// </returns>
        Task RawProduceAsync(
            string actualEndpointName,
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task" /> completes when the message is enqueued while the callbacks
        ///     are called when the message is actually produced (or the produce failed).
        /// </remarks>
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <param name="messageStream">
        ///     The message to be delivered.
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
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The <see cref="Task" /> will complete as
        ///     soon as the message is enqueued.
        /// </returns>
        Task RawProduceAsync(
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default);
    }
}
