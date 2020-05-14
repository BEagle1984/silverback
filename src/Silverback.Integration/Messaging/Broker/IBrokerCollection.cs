// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Holds a reference to all the registered <see cref="IBroker" /> implementations and is able to
    ///     resolve the right instance according to the <see cref="IEndpoint" /> type.
    /// </summary>
    public interface IBrokerCollection : IReadOnlyCollection<IBroker>
    {
        /// <summary>
        ///     Returns an <see cref="IProducer" /> to be used to produce to the specified endpoint.
        /// </summary>
        /// <param name="endpoint"> The target endpoint. </param>
        /// <returns>
        ///     The <see cref="IProducer" /> for the specified endpoint.
        /// </returns>
        IProducer GetProducer(IProducerEndpoint endpoint);

        /// <summary>
        ///     Returns an <see cref="IConsumer" /> to be used to consume from the specified endpoint.
        /// </summary>
        /// <param name="endpoint"> The source endpoint. </param>
        /// <param name="callback"> The delegate to be invoked when a message is received. </param>
        /// <returns>
        ///     The <see cref="IConsumer" /> for the specified endpoint.
        /// </returns>
        IConsumer GetConsumer(IConsumerEndpoint endpoint, MessagesReceivedCallback callback);

        /// <summary>
        ///     Returns an <see cref="IConsumer" /> to be used to consume from the specified endpoint.
        /// </summary>
        /// <param name="endpoint"> The source endpoint. </param>
        /// <param name="callback"> The delegate to be invoked when a message is received. </param>
        /// <returns>
        ///     The <see cref="IConsumer" /> for the specified endpoint.
        /// </returns>
        IConsumer GetConsumer(IConsumerEndpoint endpoint, MessagesReceivedAsyncCallback callback);

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
