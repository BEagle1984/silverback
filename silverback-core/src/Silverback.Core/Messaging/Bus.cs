using Silverback.Extensions;
using Silverback.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging
{
    /// <summary>
    /// The standard in-process bus.
    /// </summary>
    public class Bus : IBus, IDisposable
    {
        private readonly List<ISubscriber> _subscribers = new List<ISubscriber>();
        private readonly ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();
        private bool _disposed = false;

        internal Bus()
        {
        }

        #region Publish

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public void Publish(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            CheckDisposed();

            _subscribers.ForEach(s => s.OnNext(message));
        }

        /// <summary>
        /// Asynchronously publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">message</exception>
        public Task PublishAsync(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            CheckDisposed();

            return _subscribers.ForEachAsync(async s => await s.OnNextAsync(message));
        }

        #endregion

        #region Subscribe / Unsubscribe

        /// <summary>
        /// Subscribes the specified <see cref="T:Silverback.Messaging.ISubscriber" /> to receive
        /// the messages sent through this bus.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        public ISubscriber Subscribe(ISubscriber subscriber)
        {
            CheckDisposed();

            lock (_subscribers)
            {
                _subscribers.Add(subscriber);
            }

            return subscriber;
        }

        /// <summary>
        /// Dispose the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        public void Unsubscribe(ISubscriber subscriber)
        {
            lock (_subscribers)
            {
                _subscribers.Remove(subscriber);
            }
        }
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
        public ConcurrentDictionary<string, object> Items
        {
            get
            {
                CheckDisposed();
                return _items;
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Throws an exception if the current instance was disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                lock (_subscribers)
                {
                    _subscribers?.OfType<IDisposable>().ForEach(s => s.Dispose());
                }

                _items?.ForEach(i => (i.Value as IDisposable)?.Dispose());

                _disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Bus"/> class.
        /// </summary>
        ~Bus()
        {
            Dispose(false);
        }

        #endregion
    }
}