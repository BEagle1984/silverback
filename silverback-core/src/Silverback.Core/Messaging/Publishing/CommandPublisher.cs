using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    /// Publishes the <see cref="ICommand"/> to the bus.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <seealso cref="Silverback.Messaging.Publishing.ICommandPublisher{TCommand}" />
    public class CommandPublisher<TCommand> : ICommandPublisher<TCommand>
        where TCommand : ICommand
    {
        private readonly IPublisher _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPublisher{TCommand}"/> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        public CommandPublisher(IBus bus)
        {
            _publisher = new Publisher(bus);
        }


        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <param name="message">The command to be sent.</param>
        public void Send(TCommand message)
            => _publisher.Send(message);

        /// <summary>
        /// Sends the specified command to the bus.
        /// </summary>
        /// <param name="message">The command to be sent.</param>
        /// <returns></returns>
        public Task SendAsync(TCommand message)
            => _publisher.SendAsync(message);
    }
}