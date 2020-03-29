// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Holds a reference to all the registered <see cref="IBroker" /> implementations and
    ///     is able to resolve the right instance according to the <see cref="IEndpoint" /> type.
    /// </summary>
    public interface IBrokerCollection : IReadOnlyCollection<IBroker>
    {
        /// <summary>
        ///     Returns an <see cref="IProducer" /> to be used to produce to
        ///     the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The target endpoint.
        /// </param>
        /// <param name="behaviors">
        ///     A collection of behaviors to be added to this producer instance in addition to the ones registered for
        ///     dependency injection.
        /// </param>
        IProducer GetProducer(IProducerEndpoint endpoint, IReadOnlyCollection<IProducerBehavior> behaviors = null);

        /// <summary>
        ///     Returns an <see cref="IConsumer" /> to be used to consume from
        ///     the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The source endpoint.
        /// </param>
        /// <param name="behaviors">
        ///     A collection of behaviors to be added to this consumer instance in addition to the ones registered for
        ///     dependency injection.
        /// </param>
        IConsumer GetConsumer(IConsumerEndpoint endpoint, IReadOnlyCollection<IConsumerBehavior> behaviors = null);

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