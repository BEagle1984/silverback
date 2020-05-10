// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    internal abstract class RawBrokerEnvelope : IRawBrokerEnvelope
    {
        protected RawBrokerEnvelope(
            byte[] rawMessage,
            IEnumerable<MessageHeader> headers,
            IEndpoint endpoint,
            IOffset offset)
        {
            RawMessage = rawMessage;
            Headers = new MessageHeaderCollection(headers);
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));
            Offset = offset;
        }

        public byte[]? RawMessage { get; set; }

        public MessageHeaderCollection Headers { get; set; }

        public IOffset Offset { get; internal set; }

        public IEndpoint Endpoint { get; }
    }
}
