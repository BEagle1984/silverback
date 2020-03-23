// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class ProducedMessage
    {
        public ProducedMessage(byte[] message, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
        {
            Message = message;

            if (headers != null)
                Headers.AddRange(headers);

            Endpoint = endpoint;
        }

        public byte[] Message { get; }
        public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();
        public IEndpoint Endpoint { get; }
    }
}