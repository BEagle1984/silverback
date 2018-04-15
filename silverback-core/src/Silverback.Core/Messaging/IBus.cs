using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

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
        /// Subscribes to the messages stream. The function must return an <see cref="IDisposable" />
        /// to let the <see cref="IBus" /> handle the subscriber lifecycle.
        /// </summary>
        /// <param name="subscription">The method performing the subscription.</param>
        /// <returns>
        /// Returns the subscriber.
        /// </returns>
        IDisposable Subscribe(Func<IObservable<IMessage>, IDisposable> subscription);

        /// <summary>
        /// Dispose the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        void Unsubscribe(IDisposable subscriber);

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
