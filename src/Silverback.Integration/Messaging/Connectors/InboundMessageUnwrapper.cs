// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    public class InboundMessageUnwrapper : ISubscriber
    {
        private readonly IPublisher _publisher;

        public InboundMessageUnwrapper(IPublisher publisher)
        {
            _publisher = publisher;
        }

        [Subscribe]
        public void OnMessageReceived(IInboundMessage message)
        {
            if (message.MustUnwrap)
                _publisher.Publish(message.Content);
        }
    }
}
