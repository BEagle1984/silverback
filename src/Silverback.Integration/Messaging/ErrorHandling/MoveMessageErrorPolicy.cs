// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     This policy moves the failed messages to the configured endpoint.
    /// </summary>
    public class MoveMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly IProducer _producer;

        private readonly IProducerEndpoint _endpoint;

        private readonly ILogger _logger;

        private Action<IOutboundEnvelope, Exception>? _transformationAction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MoveMessageErrorPolicy" /> class.
        /// </summary>
        /// <param name="brokerCollection">
        ///     The collection containing the available brokers.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to move the message to.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        public MoveMessageErrorPolicy(
            IBrokerCollection brokerCollection,
            IProducerEndpoint endpoint,
            IServiceProvider serviceProvider,
            ILogger<MoveMessageErrorPolicy> logger)
            : base(serviceProvider, logger)
        {
            Check.NotNull(brokerCollection, nameof(brokerCollection));
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _producer = brokerCollection.GetProducer(endpoint);
            _endpoint = endpoint;
            _logger = logger;
        }

        /// <summary>
        ///     Defines an <see cref="Action{T}" /> to be called to modify (or completely rewrite) the message being
        ///     moved.
        /// </summary>
        /// <param name="transformationAction">
        ///     The <see cref="Action{T}" /> to be called to modify the message. This function can be used to modify
        ///     or replace the message body and its headers.
        /// </param>
        /// <returns>
        ///     The <see cref="MoveMessageErrorPolicy" /> so that additional calls can be chained.
        /// </returns>
        public MoveMessageErrorPolicy Transform(Action<IOutboundEnvelope, Exception> transformationAction)
        {
            _transformationAction = transformationAction;
            return this;
        }

        /// <inheritdoc cref="ErrorPolicyBase.ApplyPolicy" />
        protected override async Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            Check.NotNull(envelopes, nameof(envelopes));

            _logger.LogInformation(
                EventIds.MoveMessageErrorPolicyMoveMessages,
                $"{envelopes.Count} message(s) will be  be moved to endpoint '{_endpoint.Name}'.",
                envelopes);

            await envelopes.ForEachAsync(envelope => PublishToNewEndpoint(envelope, exception));

            return ErrorAction.Skip;
        }

        private async Task PublishToNewEndpoint(IRawInboundEnvelope envelope, Exception exception)
        {
            envelope.Headers.AddOrReplace(
                DefaultMessageHeaders.SourceEndpoint,
                envelope.Endpoint?.Name ?? string.Empty);

            IOutboundEnvelope? outboundEnvelope =
                envelope is IInboundEnvelope deserializedEnvelope
                    ? new OutboundEnvelope(deserializedEnvelope.Message, deserializedEnvelope.Headers, _endpoint)
                    : new OutboundEnvelope(envelope.RawMessage, envelope.Headers, _endpoint);

            _transformationAction?.Invoke((OutboundEnvelope)outboundEnvelope, exception);

            await _producer.ProduceAsync(outboundEnvelope);
        }
    }
}
