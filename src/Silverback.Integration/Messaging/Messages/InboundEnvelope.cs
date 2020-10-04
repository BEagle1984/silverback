// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawInboundEnvelope" />
    internal class InboundEnvelope : RawInboundEnvelope, IInboundEnvelope
    {
        public InboundEnvelope(IRawInboundEnvelope envelope)
            : this(
                envelope.RawMessage,
                envelope.Headers,
                envelope.Offset,
                envelope.Endpoint,
                envelope.ActualEndpointName,
                envelope.AdditionalLogData,
                envelope.Sequence)
        {
        }

        public InboundEnvelope(
            Stream? rawMessage,
            IEnumerable<MessageHeader>? headers,
            IOffset? offset,
            IConsumerEndpoint endpoint,
            string actualEndpointName,
            IDictionary<string, string>? additionalLogData = null,
            ISequence? sequence = null)
            : base(rawMessage, headers, endpoint, actualEndpointName, offset, additionalLogData, sequence)
        {
        }

        public bool AutoUnwrap { get; } = true;

        public object? Message { get; set; }
    }
}
