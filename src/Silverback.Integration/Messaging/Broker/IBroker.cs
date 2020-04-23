// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The basic interface to interact with the message broker.
    /// </summary>
    public interface IBroker
    {
        /// <summary>
        ///     Gets the type of the <see cref="IProducerEndpoint" /> that is being handled by this broker
        ///     implementation.
        /// </summary>
        Type ProducerEndpointType { get; }

        /// <summary>
        ///     Gets the type of the <see cref="IConsumerEndpoint" /> that is being handled by this broker
        ///     implementation.
        /// </summary>
        Type ConsumerEndpointType { get; }

        /// <summary>
        ///     Gets the collection of <see cref="IProducer" /> that have been created so far.
        /// </summary>
        IReadOnlyList<IProducer> Producers { get; }

        /// <summary>
        ///     Gets the collection of <see cref="IConsumer" /> that have been created so far.
        /// </summary>
        IReadOnlyList<IConsumer> Consumers { get; }

        /// <summary>
        ///     Gets a value indicating whether this broker is currently connected.
        /// </summary>
        bool IsConnected { get; }

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
        ///     Connect to the message broker to start consuming.
        /// </summary>
        void Connect();

        /// <summary>
        ///     Disconnect from the message broker to stop consuming.
        /// </summary>
        void Disconnect();
    }
}
