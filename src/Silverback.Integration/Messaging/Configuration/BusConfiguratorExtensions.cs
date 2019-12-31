// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    public static class BusConfiguratorExtensions
    {
        /// <summary>
        ///     Configures the message broker endpoints and start consuming.
        /// </summary>
        /// <param name="configurator"></param>
        /// <returns></returns>
        public static IBroker Connect(this BusConfigurator configurator) =>
            Connect(configurator, null);

        /// <summary>
        ///     Configures the message broker endpoints and start consuming.
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="endpointsConfigurationAction">The inbound/outbound endpoints configuration action.</param>
        /// <returns></returns>
        public static IBroker Connect(
            this BusConfigurator configurator,
            Action<IEndpointsConfigurationBuilder> endpointsConfigurationAction)
        {
            var endpointsConfigurationBuilder = new EndpointsConfigurationBuilder(
                configurator.ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                configurator.ServiceProvider.GetRequiredService<IEnumerable<IInboundConnector>>(),
                configurator.ServiceProvider.GetRequiredService<ErrorPolicyBuilder>());
            endpointsConfigurationAction?.Invoke(endpointsConfigurationBuilder);

            configurator.ServiceProvider.GetServices<IEndpointsConfigurator>().ForEach(c =>
                c.Configure(endpointsConfigurationBuilder));

            var broker = configurator.ServiceProvider.GetRequiredService<IBroker>();

            broker.Connect();

            return broker;
        }
    }
}