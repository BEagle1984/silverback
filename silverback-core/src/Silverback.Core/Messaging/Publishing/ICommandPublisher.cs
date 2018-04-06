using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="ICommand"/> to the bus.
    /// </summary>
    public interface ICommandPublisher<in TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <param name="message">The command to be sent.</param>
        void Send(TCommand message);

        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <param name="message">The command to be sent.</param>
        /// <returns></returns>
        Task SendAsync(TCommand message);
    }
}