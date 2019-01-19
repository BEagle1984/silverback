// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Connectors.Repositories
{
    public class QueuedMessage
    {
        public QueuedMessage(object message, IEndpoint endpoint)
        {
            Message = message;
            Endpoint = endpoint;
        }

        public object Message { get; }

        public IEndpoint Endpoint { get; }
    }
}
