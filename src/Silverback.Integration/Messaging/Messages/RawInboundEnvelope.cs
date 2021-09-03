// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawInboundEnvelope" />
    internal class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
    {
        private readonly string? _actualEndpointDisplayName;

        public RawInboundEnvelope(
            byte[]? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IBrokerMessageIdentifier brokerMessageIdentifier)
            : this(
                rawMessage != null ? new MemoryStream(rawMessage) : null,
                headers,
                endpoint,
                actualEndpointName,
                brokerMessageIdentifier)
        {
        }

        public RawInboundEnvelope(
            Stream? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IBrokerMessageIdentifier brokerMessageIdentifier)
            : base(rawMessage, headers, endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            ActualEndpointName = actualEndpointName;
            BrokerMessageIdentifier = brokerMessageIdentifier;

            if (endpoint.FriendlyName != null)
                _actualEndpointDisplayName = $"{endpoint.FriendlyName} ({actualEndpointName})";
        }

        public new IConsumerEndpoint Endpoint => (IConsumerEndpoint)base.Endpoint;

        public string ActualEndpointName { get; }

        public string ActualEndpointDisplayName => _actualEndpointDisplayName ?? ActualEndpointName;

        public IBrokerMessageIdentifier BrokerMessageIdentifier { get; }
    }
}
