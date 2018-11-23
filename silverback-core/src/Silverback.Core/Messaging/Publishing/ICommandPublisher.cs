using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface ICommandPublisher<in TCommand>
        where TCommand : ICommand
    {
        void Send(TCommand commandMessage);

        Task SendAsync(TCommand commandMessage);
    }
}