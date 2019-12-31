// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public Task OnMessagesReceived(IEnumerable<IInboundMessage> messages) =>
            _publisher.PublishAsync(
                messages
                    .Where(m => m.MustUnwrap)
                    .Select(m => m.Content)
                    .ToList());
    }
}