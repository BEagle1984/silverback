// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>AddInbound</c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    /// <remarks>
    ///     Multiple calls to this methods will cause multiple consumers to be instantiated, which could mean
    ///     multiple connections being issues and more resources being used (depending on the actual message broker
    ///     implementation). The consumer endpoint might allow to define multiple endpoints at once, to efficiently
    ///     instantiate a single consumer for all of them.
    /// </remarks>
    public static class EndpointsConfigurationBuilderAddInboundExtensions
    {
        /// <summary>
        ///     Adds an inbound endpoint and instantiates a consumer.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint (topic).
        /// </param>
        /// <param name="consumersCount">
        ///     The number of consumers to be instantiated. The default is 1.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddInbound(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            IConsumerEndpoint endpoint,
            int consumersCount = 1)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(endpoint, nameof(endpoint));

            if (consumersCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(consumersCount),
                    consumersCount,
                    "The consumers count must be greater or equal to 1.");
            }

            var serviceProvider = endpointsConfigurationBuilder.ServiceProvider;
            var brokerCollection = serviceProvider.GetRequiredService<IBrokerCollection>();

            for (int i = 0; i < consumersCount; i++)
            {
                brokerCollection.AddConsumer(endpoint);
            }

            return endpointsConfigurationBuilder;
        }
    }
}
