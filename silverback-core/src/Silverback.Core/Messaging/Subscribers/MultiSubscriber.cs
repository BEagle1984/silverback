using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Extensions;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus and executes the specified action for each message.
    /// </summary>
    /// <seealso cref="AsyncSubscriber{TMessage}" />
    public abstract class MultiSubscriber : AsyncSubscriber<IMessage>
    {
        private IEnumerable<ISubscriber> _handlers;
        private IBus _bus;

        /// <summary>
        /// Initializes the subscriber.
        /// </summary>
        /// <param name="bus">The subscribed bus.</param>
        public override void Init(IBus bus)
        {
            _bus = bus;
            base.Init(bus);
        }

        /// <summary>
        /// Gets the configuration for this <see cref="MultiSubscriber"/> implementation.
        /// </summary>
        /// <remarks>
        /// The configuration is built through the Configure method and then cached.
        /// </remarks>
        private IEnumerable<ISubscriber> GetHandlers()
        {
            // TODO: Is there a way to statically cache the configuration?

            if (_handlers != null)
                return _handlers;

            var config = new MultiSubscriberConfig();
            Configure(config);
            _handlers = config.GetHandlers().ToArray();

            _handlers.ForEach(h => h.Init(_bus));

            return _handlers;
        }

        /// <summary>
        /// Configures the <see cref="MultiSubscriber"/> binding the actual message handlers methods.
        /// </summary>
        /// <param name="config">The configuration.</param>
        protected abstract void Configure(MultiSubscriberConfig config);

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" /> asynchronously.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        public override Task HandleAsync(IMessage message)
            => GetHandlers().ForEachAsync(h => h.OnNextAsync(message));
    }
}