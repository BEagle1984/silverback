// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The basic interface to interact with the message broker.
    /// </summary>
    public interface IBroker
    {
        IProducer GetProducer(IEndpoint endpoint);

        IConsumer GetConsumer(IEndpoint endpoint);

        bool IsConnected { get; }

        void Connect();
        
        void Disconnect();
    }
}
