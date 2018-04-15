using Silverback.Extensions;
using Silverback.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Silverback.Messaging
{
    /// <summary>
    /// The standard in-process bus.
    /// </summary>
    public class Bus : IBus, IDisposable
    {
        private readonly Subject<IMessage> _subject = new Subject<IMessage>();
        private readonly List<IDisposable> _subscribers = new List<IDisposable>();
        private readonly ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        #region Publish

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public void Publish(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            _subject.OnNext(message);
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

            return Task.Run(() => _subject.OnNext(message));
        }

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
        public IDisposable Subscribe(Func<IObservable<IMessage>, IDisposable> subscription)
        {
            var subscriber = subscription(_subject);

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
        public void Unsubscribe(IDisposable subscriber)
        {
            lock (_subscribers)
            {
                subscriber.Dispose();

                if (_subscribers.Contains(subscriber))
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
        public ConcurrentDictionary<string, object> Items => _items;

        #endregion

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all subscribers and the subject
                lock (_subscribers)
                {
                    _subscribers?.ForEach(s => s.Dispose());
                    _items?.ForEach(i => (i.Value as IDisposable)?.Dispose());
                }
                _subject?.Dispose();
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
    }
}