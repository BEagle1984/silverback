// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class QueuedMessage
    {
        public QueuedMessage(object message, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
        {
            Message = message;
            Headers = headers;
            Endpoint = endpoint;
        }

        public object Message { get; }

        public IEnumerable<MessageHeader> Headers { get; }

        public IEndpoint Endpoint { get; }
    }
}
