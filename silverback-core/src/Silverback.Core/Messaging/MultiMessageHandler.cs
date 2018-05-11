using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Extensions;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// Handles instances of <see cref="IMessage"/> of multiple subtypes.
    /// </summary>
    /// <seealso cref="IMessage" />
    public abstract class MultiMessageHandler : MessageHandler<IMessage>
    {
        private static readonly ConcurrentDictionary<Type, IMessageHandler[]> _handlers = new ConcurrentDictionary<Type, IMessageHandler[]>();

        /// <summary>
        /// Gets the configuration for this <see cref="MultiMessageHandler"/> implementation.
        /// </summary>
        /// <remarks>
        /// The configuration is built through the Configure method and then cached.
        /// </remarks>
        private IEnumerable<IMessageHandler> GetHandlers()
            => _handlers.GetOrAdd(GetType(), t =>
            {
                var config = new MultiMessageHandlerConfiguration();
                Configure(config);
                return config.GetHandlers().ToArray();
            });

        /// <summary>
        /// Configures the <see cref="MultiMessageHandler"/> binding the actual message handlers methods.
        /// </summary>
        /// <param name="config">The configuration.</param>
        protected abstract void Configure(MultiMessageHandlerConfiguration config);

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public override void Handle(IMessage message)
        {
            // TODO: Should simply subscribe all methods using a generichandler
            GetHandlers().ForEach(h => h.Handle(message));
        }
    }
}