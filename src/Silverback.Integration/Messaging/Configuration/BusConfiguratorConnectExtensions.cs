// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c> Connect </c> method to the <see cref="IBusConfigurator" />.
    /// </summary>
    public static class BusConfiguratorConnectExtensions
    {
        /// <summary>
        ///     Configures the message broker endpoints and start consuming.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the internal bus.
        /// </param>
        /// <returns>
        ///     An <see cref="IBrokerCollection" /> collection containing the broker instances that have been
        ///     connected.
        /// </returns>
        public static IBrokerCollection Connect(this IBusConfigurator busConfigurator) =>
            Connect(busConfigurator, null);

        /// <summary>
        ///     Configures the message broker endpoints and start consuming.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the internal bus.
        /// </param>
        /// <param name="endpointsConfigurationAction">
        ///     The inbound/outbound endpoints configuration action.
        /// </param>
        /// <returns>
        ///     An <see cref="IBrokerCollection" /> collection containing the broker instances that have been
        ///     connected.
        /// </returns>
        public static IBrokerCollection Connect(
            this IBusConfigurator busConfigurator,
            Action<IEndpointsConfigurationBuilder>? endpointsConfigurationAction)
        {
            if (busConfigurator == null)
                throw new ArgumentNullException(nameof(busConfigurator));

            var endpointsConfigurationBuilder = new EndpointsConfigurationBuilder(busConfigurator.ServiceProvider);

            endpointsConfigurationAction?.Invoke(endpointsConfigurationBuilder);

            busConfigurator.ServiceProvider.GetServices<IEndpointsConfigurator>()
                .ForEach(endpointsConfigurator => endpointsConfigurator.Configure(endpointsConfigurationBuilder));

            var brokers = busConfigurator.ServiceProvider.GetRequiredService<IBrokerCollection>();

            brokers.Connect();

            return brokers;
        }
    }
}
