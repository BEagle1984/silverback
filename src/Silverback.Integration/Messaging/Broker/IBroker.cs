// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The basic interface to interact with the message broker.
    /// </summary>
    public interface IBroker
    {
        /// <summary>
        /// Returns an <see cref="IProducer"/> to be used to produce to
        /// the specified endpoint.
        /// </summary>
        IProducer GetProducer(IProducerEndpoint endpoint);

        /// <summary>
        /// Returns an <see cref="IConsumer"/> to be used to consume from
        /// the specified endpoint.
        /// </summary>
        IConsumer GetConsumer(IConsumerEndpoint endpoint);

        /// <summary>
        /// A boolean value indicating whether this instance is currently connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connect to the message broker to start consuming.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from the message broker to stop consuming.
        /// </summary>
        void Disconnect();
    }
}
