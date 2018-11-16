using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IPublisher
    {
        void Publish(IMessage message);

        Task PublishAsync(IMessage message);
    }
}