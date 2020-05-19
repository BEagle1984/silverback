// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawInboundEnvelope" />
    internal class RawInboundEnvelope : RawBrokerEnvelope, IRawInboundEnvelope
    {
        [SuppressMessage("ReSharper", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public RawInboundEnvelope(
            byte[]? rawMessage,
            IEnumerable<MessageHeader> headers,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IOffset offset = null)
            : base(rawMessage, headers, endpoint, offset)
        {
            ActualEndpointName = actualEndpointName;
        }

        /// <inheritdoc />
        public new IConsumerEndpoint Endpoint => (IConsumerEndpoint)base.Endpoint;

        /// <inheritdoc />
        public string ActualEndpointName { get; }
    }
}
