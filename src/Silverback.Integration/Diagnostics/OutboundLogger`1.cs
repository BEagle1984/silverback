// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics
{
    internal sealed class OutboundLogger<TCategoryName>
        : SilverbackLogger<TCategoryName>, IOutboundLogger<TCategoryName>
    {
        private readonly OutboundLoggerFactory _loggerFactory;

        public OutboundLogger(
            IMappedLevelsLogger<TCategoryName> mappedLevelsLogger,
            OutboundLoggerFactory loggerFactory)
            : base(mappedLevelsLogger)
        {
            _loggerFactory = Check.NotNull(loggerFactory, nameof(loggerFactory));
        }

        public void LogProduced(IOutboundEnvelope envelope) =>
            _loggerFactory.GetOutboundLogger(envelope.Endpoint)
                .LogProduced(this, envelope);

        public void LogProduced(
            IProducerEndpoint endpoint,
            string actualEndpointName,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier) =>
            _loggerFactory.GetOutboundLogger(endpoint)
                .LogProduced(this, endpoint, actualEndpointName, headers, brokerMessageIdentifier);

        public void LogProduceError(IOutboundEnvelope envelope, Exception exception) =>
            _loggerFactory.GetOutboundLogger(envelope.Endpoint)
                .LogProduceError(this, envelope, exception);

        public void LogProduceError(
            IProducerEndpoint endpoint,
            string actualEndpointName,
            IReadOnlyCollection<MessageHeader>? headers,
            Exception exception) =>
            _loggerFactory.GetOutboundLogger(endpoint)
                .LogProduceError(
                    this,
                    endpoint,
                    actualEndpointName,
                    headers,
                    exception);

        public void LogWrittenToOutbox(IOutboundEnvelope envelope) =>
            _loggerFactory.GetOutboundLogger(envelope.Endpoint)
                .LogWrittenToOutbox(this, envelope);

        public void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception) =>
            _loggerFactory.GetOutboundLogger(envelope.Endpoint)
                .LogErrorProducingOutboxStoredMessage(this, envelope, exception);
    }
}
