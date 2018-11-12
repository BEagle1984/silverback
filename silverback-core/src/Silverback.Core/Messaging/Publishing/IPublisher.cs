using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IPublisher
    {
        void Publish<TMessage>(TMessage message)
            where TMessage : IMessage;

        Task PublishAsync<TMessage>(TMessage message)
            where TMessage : IMessage;
    }
}