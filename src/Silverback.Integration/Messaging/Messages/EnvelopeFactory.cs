// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Can be used to instantiate an <see cref="IOutboundEnvelope" /> or an <see cref="IInboundEnvelope" />.
    /// </summary>
    public static class EnvelopeFactory
    {
        /// <summary>
        ///     Creates an <see cref="IOutboundEnvelope{TMessage}" />.
        /// </summary>
        /// <param name="message">
        ///     The message to be wrapped in the envelope.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpoint">
        ///     The destination endpoint.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the message being wrapped.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IOutboundEnvelope{TMessage}" /> containing the specified message.
        /// </returns>
        public static IOutboundEnvelope<TMessage> Create<TMessage>(
            TMessage message,
            MessageHeaderCollection? headers,
            IProducerEndpoint endpoint)
            where TMessage : class =>
            new OutboundEnvelope<TMessage>(message, headers, endpoint);

        /// <summary>
        ///     Creates an <see cref="IRawInboundEnvelope" />.
        /// </summary>
        /// <param name="rawMessage">
        ///     The raw message body.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpoint">
        ///     The source endpoint.
        /// </param>
        /// <param name="identifier">
        ///     The <see cref="IBrokerMessageIdentifier" />.
        /// </param>
        /// <returns>
        ///     An <see cref="IInboundEnvelope{TMessage}" /> containing the specified message.
        /// </returns>
        public static IRawInboundEnvelope Create(
            byte[]? rawMessage,
            MessageHeaderCollection? headers,
            IConsumerEndpoint endpoint,
            IBrokerMessageIdentifier identifier) =>
            new RawInboundEnvelope(
                rawMessage,
                headers?.AsReadOnlyCollection(),
                endpoint,
                Check.NotNull(endpoint, nameof(endpoint)).Name,
                identifier);

        /// <summary>
        ///     Creates an <see cref="IRawInboundEnvelope" />.
        /// </summary>
        /// <param name="rawMessageStream">
        ///     The raw message body.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpoint">
        ///     The source endpoint.
        /// </param>
        /// <param name="identifier">
        ///     The <see cref="IBrokerMessageIdentifier" />.
        /// </param>
        /// <returns>
        ///     An <see cref="IInboundEnvelope{TMessage}" /> containing the specified message.
        /// </returns>
        public static IRawInboundEnvelope Create(
            Stream rawMessageStream,
            MessageHeaderCollection? headers,
            IConsumerEndpoint endpoint,
            IBrokerMessageIdentifier identifier) =>
            new RawInboundEnvelope(
                rawMessageStream,
                headers?.AsReadOnlyCollection(),
                endpoint,
                Check.NotNull(endpoint, nameof(endpoint)).Name,
                identifier);

        /// <summary>
        ///     Creates an <see cref="IInboundEnvelope{TMessage}" /> copying another envelope and replacing just the deserialized message.
        /// </summary>
        /// <param name="message">
        ///     The message to be wrapped in the envelope.
        /// </param>
        /// <param name="rawInboundEnvelope">
        ///     The envelope to be copied.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The type of the message being wrapped.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IInboundEnvelope{TMessage}" /> containing the specified message.
        /// </returns>
        public static IInboundEnvelope<TMessage> Create<TMessage>(
            TMessage message,
            IRawInboundEnvelope rawInboundEnvelope)
            where TMessage : class =>
            new InboundEnvelope<TMessage>(rawInboundEnvelope, message);
    }
}
