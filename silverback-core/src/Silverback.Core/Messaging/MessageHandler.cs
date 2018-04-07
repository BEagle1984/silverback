using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// Handles the <see cref="IMessage"/> of type <see cref="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of <see cref="IMessage"/> to be handled.</typeparam>
    /// <seealso cref="Silverback.Messaging.IMessageHandler" />
    public abstract class MessageHandler<TMessage> : IMessageHandler
        where TMessage : IMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandler{TMessage}"/> class.
        /// </summary>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        protected MessageHandler(Func<TMessage, bool> filter = null)
        {
            Filter = filter;
        }

        /// <summary>
        /// Gets or sets an optional filter to be applied to the messages.
        /// </summary>
        public Func<TMessage, bool> Filter { get; set; }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public abstract void Handle(TMessage message);

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        void IMessageHandler.Handle(IMessage message)
        {
            // TODO: Tracing

            if (!(message is TMessage))
                return;

            var typedMessage = (TMessage)message;

            if (Filter != null && !Filter.Invoke(typedMessage))
                return;

            Handle((TMessage)message);
        }
    }

    //public abstract class MultiMessageHandler : MessageHandler<IMessage>
    //{
    //    private static readonly ConcurrentDictionary<Type, IMessageHandler> _handlers = new ConcurrentDictionary<Type, IMessageHandler>();

    //    /// <summary>
    //    /// Gets the configuration for this <see cref="MultiMessageHandler"/> implementation.
    //    /// </summary>
    //    /// <remarks>
    //    /// The configuration is built through the Configure method and then cached.
    //    /// </remarks>
    //    private MultiMessageHandlerConfiguration GetConfiguration()
    //        => _handlers.GetOrAdd(GetType(), t =>
    //        {
    //            var config = new MultiMessageHandlerConfiguration();
    //            Configure(config);
    //            return config;
    //        });

    //    /// <summary>
    //    /// Configures the <see cref="MultiMessageHandler"/> binding the actual message handlers methods.
    //    /// </summary>
    //    /// <param name="config">The configuration.</param>
    //    protected abstract void Configure(MultiMessageHandlerConfiguration config);

    //    /// <summary>
    //    /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
    //    /// </summary>
    //    /// <param name="message">The message to be handled.</param>
    //    public override void Handle(IMessage message)
    //    {
    //        var config = GetConfiguration();

    //        while (true)
    //        {
    //            var message = (IMessage)null;

    //            var type = typeof(IEvent);
    //            var action = new Action<IEvent>(e => {});
    //            var filter = new Func<IEvent, bool>(e => true);

    //            if (message.GetType().IsAssignableFrom(type))
    //            {
    //                if ((bool)filter.DynamicInvoke(message))
    //                    action.DynamicInvoke(message);
    //            }

    //            Action<IMessage> test = t => HandleEvent(t);
    //        }
    //    }

    //    public void HandleEvent(IEvent e) { }
    //}

    //public class MultiMessageHandlerConfiguration
    //{
    //    private readonly List<IMessageHandler> _config = new List<IMessageHandler>();

    //    public MultiMessageHandlerConfiguration Handle<TMessage>(Action<TMessage> handler, Func<TMessage, bool> filter = null)
    //        where TMessage : IMessage
    //    {
    //        _config.Add(new GenericMessageHandler<TMessage>(handler, filter));

    //        return this;
    //    }
    //}
}
