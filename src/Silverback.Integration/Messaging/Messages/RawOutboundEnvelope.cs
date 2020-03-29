// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class RawOutboundEnvelope : RawBrokerEnvelope, IRawOutboundEnvelope
    {
        public RawOutboundEnvelope(
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            IOffset offset = null)
            : base(headers, endpoint, offset)
        {
        }

        public RawOutboundEnvelope(
            byte[] rawMessage,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            IOffset offset = null)
            : base(rawMessage, headers, endpoint, offset)
        {
        }

        public new IProducerEndpoint Endpoint => (IProducerEndpoint) base.Endpoint;
    }
}