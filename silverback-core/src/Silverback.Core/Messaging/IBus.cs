using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging
{
    /// <summary>
    /// A publish-subscribe based observable bus.
    /// </summary>
    public interface IBus
    {
        #region Publish

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        void Publish(IMessage message);

        /// <summary>
        /// Asynchronously publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        /// <returns></returns>
        Task PublishAsync(IMessage message);

        #endregion

        #region Subscribe / Unsubscribe

        /// <summary>
        /// Subscribes the specified <see cref="ISubscriber"/> to receive
        /// the messages sent through this bus.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        ISubscriber Subscribe(ISubscriber subscriber);

        /// <summary>
        /// Unsubscribes the specified <see cref="ISubscriber"/> to stop receiving
        /// the messages sent through this bus.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        void Unsubscribe(ISubscriber subscriber);

        #endregion

        #region Items

        /// <summary>
        /// Gets a dictionary to store objects related to the <see cref="IBus"/>.
        /// The lifecycle of these objects will be bound to the bus and all objects
        /// implementing <see cref="IDisposable"/> will be disposed with it.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        ConcurrentDictionary<string, object> Items { get; }

        #endregion
    }
}
