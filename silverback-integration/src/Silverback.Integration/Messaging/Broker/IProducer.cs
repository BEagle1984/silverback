    // Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public interface IProducer
    {
        void Produce(IMessage message);

        Task ProduceAsync(IMessage message);
    }
}