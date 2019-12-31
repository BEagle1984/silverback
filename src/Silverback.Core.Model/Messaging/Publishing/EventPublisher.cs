// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IPublisher _publisher;

        public EventPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Publish(IEvent eventMessage) => _publisher.Publish(eventMessage);

        public Task PublishAsync(IEvent eventMessage) => _publisher.PublishAsync(eventMessage);

        public void Publish(IEnumerable<IEvent> eventMessages) => _publisher.Publish(eventMessages);

        public Task PublishAsync(IEnumerable<IEvent> eventMessages) => _publisher.PublishAsync(eventMessages);
    }
}