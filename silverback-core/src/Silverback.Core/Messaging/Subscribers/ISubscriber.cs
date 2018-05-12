using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus.
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// Called when a message is published.
        /// </summary>
        /// <param name="message">The message.</param>
        void OnNext(IMessage message);

        /// <summary>
        /// Called when a message is published asynchronously.
        /// </summary>
        /// <param name="message">The message.</param>
        Task OnNextAsync(IMessage message);
    }
}