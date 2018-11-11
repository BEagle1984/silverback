using Silverback.Util;
using Silverback.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public void Publish(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            _subscribers.ForEach(s => s.OnNext(message));
        }

        public Task PublishAsync(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            ThrowIfDisposed();

            return _subscribers.ForEachAsync(async s => await s.OnNextAsync(message));
        }

        #endregion

        #region Subscribe / Unsubscribe

        public IBus Subscribe(ISubscriber subscriber)
        {
            ThrowIfDisposed();

            lock (_subscribers)
            {
                _subscribers.Add(subscriber);
                subscriber.Init(this);
            }

            return this;
        }

        public IBus Unsubscribe(ISubscriber subscriber)
        {
            lock (_subscribers)
            {
                _subscribers.Remove(subscriber);
            }

            return this;
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
                ThrowIfDisposed();
                return _items;
            }
        }

        #endregion

        #region Dispose

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Bus()
        {
            Dispose(false);
        }

        #endregion
    }
}