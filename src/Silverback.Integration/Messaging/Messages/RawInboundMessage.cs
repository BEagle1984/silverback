// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public class RawInboundMessage : RawBrokerMessage, IRawInboundMessage
    {
        public RawInboundMessage(
            object content,
            IEnumerable<MessageHeader> headers,
            IConsumerEndpoint endpoint,
            IOffset offset = null)
            : base(content, headers, endpoint, offset)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public RawInboundMessage(
            byte[] rawContent,
            IEnumerable<MessageHeader> headers,
            IConsumerEndpoint endpoint,
            IOffset offset = null)
            : base(rawContent, headers, endpoint, offset)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public new IConsumerEndpoint Endpoint { get; }
    }
}