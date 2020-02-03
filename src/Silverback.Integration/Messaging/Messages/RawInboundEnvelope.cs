// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
    {
        public RawInboundEnvelope(
            object message,
            IEnumerable<MessageHeader> headers,
            IConsumerEndpoint endpoint,
            IOffset offset = null)
            : base(message, headers, endpoint, offset)
        {
        }

        public RawInboundEnvelope(
            byte[] rawMessage,
            IEnumerable<MessageHeader> headers,
            IConsumerEndpoint endpoint,
            IOffset offset = null)
            : base(rawMessage, headers, endpoint, offset)
        {
        }

        public new IConsumerEndpoint Endpoint => (IConsumerEndpoint) base.Endpoint;
    }
}