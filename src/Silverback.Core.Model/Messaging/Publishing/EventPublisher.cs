// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc cref="IEventPublisher" />
    public class EventPublisher : IEventPublisher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventPublisher" /> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be wrapped.
        /// </param>
        public EventPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <inheritdoc cref="IEventPublisher.Publish(IEvent)" />
        public void Publish(IEvent eventMessage) => _publisher.Publish(eventMessage);

        /// <inheritdoc cref="IEventPublisher.Publish(IEnumerable{IEvent})" />
        public void Publish(IEnumerable<IEvent> eventMessages) => _publisher.Publish(eventMessages);

        /// <inheritdoc cref="IEventPublisher.PublishAsync(IEvent)" />
        public Task PublishAsync(IEvent eventMessage) => _publisher.PublishAsync(eventMessage);

        /// <inheritdoc cref="IEventPublisher.PublishAsync(IEnumerable{IEvent})" />
        public Task PublishAsync(IEnumerable<IEvent> eventMessages) => _publisher.PublishAsync(eventMessages);
    }
}
