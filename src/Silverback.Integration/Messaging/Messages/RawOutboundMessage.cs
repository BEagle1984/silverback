// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public class RawOutboundMessage : RawBrokerMessage, IRawOutboundMessage
    {
        public RawOutboundMessage(
            object content,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            IOffset offset = null)
            : base(content, headers, endpoint, offset)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public RawOutboundMessage(
            byte[] rawContent,
            IEnumerable<MessageHeader> headers,
            IProducerEndpoint endpoint,
            IOffset offset = null)
            : base(rawContent, headers, endpoint, offset)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public new IProducerEndpoint Endpoint { get; }
    }
}