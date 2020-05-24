// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc />
    public class EventPublisher : IEventPublisher
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventPublisher"/> class.
        /// </summary>
        /// <param name="publisher">
        ///     The <see cref="IPublisher" /> to be wrapped.
        /// </param>
        public EventPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <inheritdoc />
        public void Publish(IEvent eventMessage) => _publisher.Publish(eventMessage);

        /// <inheritdoc />
        public void Publish(IEnumerable<IEvent> eventMessages) => _publisher.Publish(eventMessages);

        /// <inheritdoc />
        public Task PublishAsync(IEvent eventMessage) => _publisher.PublishAsync(eventMessage);

        /// <inheritdoc />
        public Task PublishAsync(IEnumerable<IEvent> eventMessages) => _publisher.PublishAsync(eventMessages);
    }
}