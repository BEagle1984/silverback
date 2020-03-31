// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    public abstract class RawBrokerEnvelope : IRawBrokerEnvelope
    {
        internal RawBrokerEnvelope(
            object message,
            IEnumerable<MessageHeader> headers,
            IEndpoint endpoint,
            IOffset offset = null)
        {
            Headers = new MessageHeaderCollection(headers);
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Offset = offset;

            RawMessage = Endpoint.Serializer.Serialize(message, Headers);
        }

        internal RawBrokerEnvelope(
            byte[] rawMessage,
            IEnumerable<MessageHeader> headers,
            IEndpoint endpoint,
            IOffset offset = null)
        {
            RawMessage = rawMessage;
            Headers = new MessageHeaderCollection(headers);
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Offset = offset;
        }

        public byte[] RawMessage { get; set; }
        public MessageHeaderCollection Headers { get; set; }
        public IOffset Offset { get; internal set; }
        public IEndpoint Endpoint { get; }
    }
}