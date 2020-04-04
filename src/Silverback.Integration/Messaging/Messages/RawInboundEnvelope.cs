// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
    {
        public RawInboundEnvelope(
            byte[] rawMessage,
            IEnumerable<MessageHeader> headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IOffset offset = null)
            : base(rawMessage, headers, endpoint, offset)
        {
            ActualEndpointName = actualEndpointName;
        }

        /// <inheritdoc cref="IRawInboundEnvelope" />
        public new IConsumerEndpoint Endpoint => (IConsumerEndpoint) base.Endpoint;

        /// <inheritdoc cref="IRawInboundEnvelope" />
        public string ActualEndpointName { get; }
    }
}