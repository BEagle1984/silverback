// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     Publishes the messages implementing <see cref="IEvent" />.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        ///     Publishes the specified event to the internal bus. The message will be forwarded to its
        ///     subscribers and the method will not complete until all subscribers have processed it (unless
        ///     using Silverback.Integration to produce and consume the message through a message broker).
        /// </summary>
        /// <param name="eventMessage"> The event to be published. </param>
        void Publish(IEvent eventMessage);

        /// <summary>
        ///     Publishes the specified event to the internal bus. The message will be forwarded to its
        ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed
        ///     it (unless using Silverback.Integration to produce and consume the message through a message
        ///     broker).
        /// </summary>
        /// <param name="eventMessage"> The event to be executed. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(IEvent eventMessage);

        /// <summary>
        ///     Publishes the specified events publishing them to the internal bus. The messages will be
        ///     forwarded to their subscribers and the method will not complete until all subscribers have
        ///     processed all messages (unless using Silverback.Integration to produce and consume the messages
        ///     through a message broker).
        /// </summary>
        /// <param name="eventMessages"> The events to be executed. </param>
        void Publish(IEnumerable<IEvent> eventMessages);

        /// <summary>
        ///     Publishes the specified events publishing them to the internal bus. The messages will be
        ///     forwarded to their subscribers and the <see cref="Task" /> will not complete until all
        ///     subscribers have processed all messages (unless using Silverback.Integration to produce and
        ///     consume the messages through a message broker).
        /// </summary>
        /// <param name="eventMessages"> The events to be executed. </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(IEnumerable<IEvent> eventMessages);
    }
}
