// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawOutboundEnvelope" />
    internal class RawOutboundEnvelope : RawBrokerEnvelope, IRawOutboundEnvelope
    {
        public RawOutboundEnvelope(
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null,
            IDictionary<string, string>? additionalLogData = null)
            : this(null, headers, endpoint, brokerMessageIdentifier, additionalLogData)
        {
        }

        public RawOutboundEnvelope(
            Stream? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null,
            IDictionary<string, string>? additionalLogData = null)
            : base(rawMessage, headers, endpoint, additionalLogData)
        {
            BrokerMessageIdentifier = brokerMessageIdentifier;
        }

        public new IProducerEndpoint Endpoint => (IProducerEndpoint)base.Endpoint;

        public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }
    }
}
