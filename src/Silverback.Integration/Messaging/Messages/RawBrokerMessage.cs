// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Messages
{
    public abstract class RawBrokerMessage : IRawBrokerMessage
    {
        internal RawBrokerMessage(object content, IEnumerable<MessageHeader> headers, IEndpoint endpoint,
            IOffset offset = null)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            Headers = new MessageHeaderCollection(headers);
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Offset = offset;

            RawContent = Endpoint.Serializer.Serialize(content, Headers);
        }

        internal RawBrokerMessage(byte[] rawContent, IEnumerable<MessageHeader> headers, IEndpoint endpoint,
            IOffset offset = null)
        {
            RawContent = rawContent ?? throw new ArgumentNullException(nameof(rawContent));
            Headers = new MessageHeaderCollection(headers);
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Offset = offset;
        }

        public byte[] RawContent { get; set; }
        public MessageHeaderCollection Headers { get; set; }
        public IOffset Offset { get; internal set; }
        public IEndpoint Endpoint { get; }
    }
}
