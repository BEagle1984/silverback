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
            ProducerEndpoint endpoint,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null)
            : this(null, headers, endpoint, brokerMessageIdentifier)
        {
        }

        public RawOutboundEnvelope(
            Stream? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            ProducerEndpoint endpoint,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null)
            : base(rawMessage, headers, endpoint)
        {
            Endpoint = endpoint;
            BrokerMessageIdentifier = brokerMessageIdentifier;
        }

        public new ProducerEndpoint Endpoint { get; internal set; }

        public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }
    }
}
