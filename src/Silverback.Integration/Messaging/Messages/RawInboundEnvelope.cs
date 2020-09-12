// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawInboundEnvelope" />
    internal class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public RawInboundEnvelope(
            byte[]? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IOffset? offset = null,
            IDictionary<string, string>? additionalLogData = null)
            : base(rawMessage, headers, endpoint, offset, additionalLogData)
        {
            ActualEndpointName = actualEndpointName;
        }

        public new IConsumerEndpoint Endpoint => (IConsumerEndpoint)base.Endpoint;

        public string ActualEndpointName { get; }

        public ISequence? Sequence { get; set; }
    }
}
