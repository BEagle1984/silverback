using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging
{
    public interface IBus
    {
        #region Publish

        void Publish(IMessage message);

        Task PublishAsync(IMessage message);

        #endregion

        #region Subscribe / Unsubscribe

        IBus Subscribe(ISubscriber subscriber);

        IBus Unsubscribe(ISubscriber subscriber);

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
