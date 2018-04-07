using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="IResponse"/> to the bus.
    /// </summary>
    public interface IResponsePublisher<in TResponse>
        where TResponse : IResponse
    {
        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <param name="message">The response.</param>
        void Reply(TResponse message);

        /// <summary>
        /// Sends the specified response to the bus.
        /// </summary>
        /// <param name="message">The response.</param>
        Task ReplyAsync(TResponse message);
    }
}
