using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// Handles the <see cref="IMessage"/> of type <see cref="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of <see cref="IMessage"/> to be handled.</typeparam>
    public interface IMessageHandler<in TMessage> : IMessageHandler
        where TMessage : IMessage
    {
        /// <summary>
        /// Handles the <see cref="IMessage"/>.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        void Handle(TMessage message);
    }

    /// <summary>
    /// Handles the <see cref="IMessage"/>.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Handles the <see cref="IMessage"/>.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        void Handle(IMessage message);
    }
}
