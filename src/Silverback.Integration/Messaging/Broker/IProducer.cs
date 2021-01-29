// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
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
        ///     Gets the <see cref="Guid" /> uniquely identifying the producer instance.
        /// </summary>
        Guid Id { get; }

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
        void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be delivered.
        /// </param>
        void Produce(IOutboundEnvelope envelope);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="messageContent">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null);

        /// <summary>
        ///     Publishes the specified message as-is, without sending it through the behaviors pipeline.
        /// </summary>
        /// <param name="messageStream">
        ///     The message to be delivered.
        /// </param>
        /// <param name="headers">
        ///     The optional message headers.
        /// </param>
        void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null);

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
        void RawProduce(
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
        void RawProduce(
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null);

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
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ProduceAsync(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null);

        /// <summary>
        ///     Publishes the specified message.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be delivered.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ProduceAsync(IOutboundEnvelope envelope);

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
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RawProduceAsync(
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
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RawProduceAsync(
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
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RawProduceAsync(
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
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RawProduceAsync(
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null);
    }
}
