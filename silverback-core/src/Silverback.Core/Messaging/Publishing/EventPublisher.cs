// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class EventPublisher<TEvent> : IEventPublisher<TEvent>
        where TEvent : IEvent
    {
        private readonly IPublisher _publisher;

        public EventPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Publish(TEvent eventMessage) => _publisher.Publish(eventMessage);

        public Task PublishAsync(TEvent eventMessage) => _publisher.PublishAsync(eventMessage);
    }
}