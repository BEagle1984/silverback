// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawInboundEnvelope" />
    internal class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
    {
        public RawInboundEnvelope(
            byte[]? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IBrokerMessageIdentifier brokerMessageIdentifier,
            IDictionary<string, string>? additionalLogData = null)
            : this(
                rawMessage != null ? new MemoryStream(rawMessage) : null,
                headers,
                endpoint,
                actualEndpointName,
                brokerMessageIdentifier,
                additionalLogData)
        {
        }

        public RawInboundEnvelope(
            Stream? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IBrokerMessageIdentifier brokerMessageIdentifier,
            IDictionary<string, string>? additionalLogData = null)
            : base(rawMessage, headers, endpoint, additionalLogData)
        {
            ActualEndpointName = actualEndpointName;
            BrokerMessageIdentifier = brokerMessageIdentifier;
        }

        public new IConsumerEndpoint Endpoint => (IConsumerEndpoint)base.Endpoint;

        public string ActualEndpointName { get; }

        public IBrokerMessageIdentifier BrokerMessageIdentifier { get; }
    }
}
