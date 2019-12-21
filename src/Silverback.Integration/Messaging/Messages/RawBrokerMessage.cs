// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public class RawBrokerMessage : IRawBrokerMessage
    {
        internal RawBrokerMessage(object content, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            Headers = new MessageHeaderCollection(headers);
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            
            RawContent = Endpoint.Serializer.Serialize(content, Headers);
        }

        internal RawBrokerMessage(byte[] rawContent, IEnumerable<MessageHeader> headers, IEndpoint endpoint, IOffset offset)
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