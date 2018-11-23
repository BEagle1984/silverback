using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public class CommandPublisher<TCommand> : ICommandPublisher<TCommand>
        where TCommand : ICommand
    {
        private readonly IPublisher _publisher;

        public CommandPublisher(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Send(TCommand commandMessage) => _publisher.Publish(commandMessage);

        public Task SendAsync(TCommand commandMessage) => _publisher.PublishAsync(commandMessage);
    }
}