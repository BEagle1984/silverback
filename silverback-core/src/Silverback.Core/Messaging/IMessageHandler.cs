using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
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
