// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawInboundEnvelope" />
    internal class InboundEnvelope : RawInboundEnvelope, IInboundEnvelope
    {
        public InboundEnvelope(IRawInboundEnvelope envelope)
            : this(
                envelope.RawMessage,
                envelope.Headers,
                envelope.BrokerMessageIdentifier,
                envelope.Endpoint,
                envelope.ActualEndpointName)
        {
        }

        public InboundEnvelope(
            Stream? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier brokerMessageIdentifier,
            IConsumerEndpoint endpoint,
            string actualEndpointName)
            : base(
                rawMessage,
                headers,
                endpoint,
                actualEndpointName,
                brokerMessageIdentifier)
        {
        }

        public InboundEnvelope(
            object message,
            Stream? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier brokerMessageIdentifier,
            IConsumerEndpoint endpoint,
            string actualEndpointName)
            : base(
                rawMessage,
                headers,
                endpoint,
                actualEndpointName,
                brokerMessageIdentifier)
        {
            Message = message;
        }

        public bool AutoUnwrap => true;

        public object? Message { get; set; }
    }
}
