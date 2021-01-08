// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class ProducedMessage
    {
        public ProducedMessage(
            Stream? message,
            IReadOnlyCollection<MessageHeader>? headers,
            IEndpoint endpoint)
        {
            Message = message;
            Headers = headers != null ? new MessageHeaderCollection(headers) : new MessageHeaderCollection();
            Endpoint = endpoint;
        }

        public Stream? Message { get; }

        public MessageHeaderCollection Headers { get; }

        public IEndpoint Endpoint { get; }
    }
}
