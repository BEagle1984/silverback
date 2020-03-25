// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    internal class InboundEnvelope : RawInboundEnvelope, IInboundEnvelope
    {
        public InboundEnvelope(IRawInboundEnvelope envelope)
            : this(envelope.RawMessage, envelope.Headers, envelope.Offset, envelope.Endpoint, envelope.ActualEndpointName)
        {
        }

        public InboundEnvelope(
            byte[] rawMessage,
            IEnumerable<MessageHeader> headers,
            IOffset offset,
            IConsumerEndpoint endpoint,
            string actualEndpointName)
            : base(rawMessage, headers, endpoint, actualEndpointName, offset)
        {
        }
        
        public object Message { get; set; }

        public bool AutoUnwrap { get; } = true;
        
    }

    internal class InboundEnvelope<TMessage> : InboundEnvelope, IInboundEnvelope<TMessage>
    {
        public InboundEnvelope(IInboundEnvelope envelope)
            : base(envelope)
        {
            if (envelope.Message != null)
                Message = (TMessage) envelope.Message;
        }

        public InboundEnvelope(
            byte[] rawContent,
            IEnumerable<MessageHeader> headers,
            IOffset offset,
            IConsumerEndpoint endpoint,
            string actualEndpointName)
            : base(rawContent, headers, offset, endpoint, actualEndpointName)
        {
        }

        public new TMessage Message
        {
            get => (TMessage) base.Message;
            set => base.Message = value;
        }
    }
}