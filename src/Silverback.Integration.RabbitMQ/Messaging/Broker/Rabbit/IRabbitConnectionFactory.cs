// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using RabbitMQ.Client;
using Silverback.Messaging.Configuration.Rabbit;

namespace Silverback.Messaging.Broker.Rabbit
{
    /// <summary>
    ///     The factory that creates and stores the connections to Rabbit in order to create a single connection
    ///     per each <see cref="RabbitConnectionConfig" />.
    /// </summary>
    internal interface IRabbitConnectionFactory : IDisposable
    {
        /// <summary>
        ///     Returns a channel to produce to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to be produced to.
        /// </param>
        /// <param name="actualEndpointName">
        ///     The actual target endpoint name.
        /// </param>
        /// <returns>
        ///     The <see cref="IModel" /> representing the channel.
        /// </returns>
        IModel GetChannel(RabbitProducerEndpoint endpoint, string actualEndpointName);

        /// <summary>
        ///     Returns a channel to consume from the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to be consumed from.
        /// </param>
        /// <returns>
        ///     The <see cref="IModel" /> representing the channel.
        /// </returns>
        (IModel Channel, string QueueName) GetChannel(RabbitConsumerEndpoint endpoint);
    }
}
