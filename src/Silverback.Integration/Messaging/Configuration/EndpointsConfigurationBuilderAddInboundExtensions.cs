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
    public static class EndpointsConfigurationBuilderAddInboundExtensions
    {
        /// <summary>
        ///     Adds an inbound endpoint.
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
