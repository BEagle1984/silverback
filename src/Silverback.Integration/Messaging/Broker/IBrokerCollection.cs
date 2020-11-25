// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Holds a reference to all the registered <see cref="IBroker" /> implementations and is able to
    ///     resolve the right instance according to the <see cref="IEndpoint" /> type.
    /// </summary>
    public interface IBrokerCollection : IReadOnlyList<IBroker>
    {
        /// <summary>
        ///     Returns an <see cref="IProducer" /> to be used to produce to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The target endpoint.
        /// </param>
        /// <returns>
        ///     The <see cref="IProducer" /> for the specified endpoint.
        /// </returns>
        IProducer GetProducer(IProducerEndpoint endpoint);

        /// <summary>
        ///     Adds an <see cref="IConsumer" /> that will consume from the specified endpoint as soon as the broker
        ///     is connected. The received messages will be forwarded to the specified callback delegate.
        /// </summary>
        /// <param name="endpoint">
        ///     The source endpoint.
        /// </param>
        /// <returns>
        ///     The <see cref="IConsumer" /> for the specified endpoint.
        /// </returns>
        IConsumer AddConsumer(IConsumerEndpoint endpoint);

        /// <summary>
        ///     Connect to all message brokers to start consuming.
        /// </summary>
        void Connect();

        /// <summary>
        ///     Disconnect from all message brokers to stop consuming.
        /// </summary>
        void Disconnect();
    }
}
