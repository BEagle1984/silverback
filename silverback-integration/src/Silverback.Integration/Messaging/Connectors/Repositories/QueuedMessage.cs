// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class QueuedMessage
    {
        public QueuedMessage(IIntegrationMessage message, IEndpoint endpoint)
        {
            Message = message;
            Endpoint = endpoint;
        }

        public IIntegrationMessage Message { get; }

        public IEndpoint Endpoint { get; }
    }
}
