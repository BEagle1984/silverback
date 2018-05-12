using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static readonly ConcurrentDictionary<Type, ISubscriber[]> _handlers = new ConcurrentDictionary<Type, ISubscriber[]>();

        /// <summary>
        /// Gets the configuration for this <see cref="MultiSubscriber"/> implementation.
        /// </summary>
        /// <remarks>
        /// The configuration is built through the Configure method and then cached.
        /// </remarks>
        private IEnumerable<ISubscriber> GetHandlers()
            => _handlers.GetOrAdd(GetType(), t =>
            {
                var config = new MultiSubscriberConfig();
                Configure(config);
                return config.GetHandlers().ToArray();
            });

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