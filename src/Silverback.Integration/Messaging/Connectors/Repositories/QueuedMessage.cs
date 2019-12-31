// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class QueuedMessage
    {
        public QueuedMessage(byte[] content, IEnumerable<MessageHeader> headers, IProducerEndpoint endpoint)
        {
            Content = content;
            Headers = headers;
            Endpoint = endpoint;
        }

        public byte[] Content { get; }

        public IEnumerable<MessageHeader> Headers { get; }

        public IProducerEndpoint Endpoint { get; }
    }
}